use anyhow::{Context, Result};
use image::{imageops::FilterType, GenericImageView, Rgba};
use std::fmt::Write as _;
use std::path::Path;

const DEFAULT_WIDTH: u32 = 40;

/// Convert a local file or URL into an ANSI-colored ASCII string built from
/// the upper-half-block character (`▀`). Foreground = top pixel, background =
/// bottom pixel — that doubles the vertical resolution per terminal row.
pub fn render(source: &str, width: u32) -> Result<String> {
    let bytes = load_bytes(source)?;
    let img = image::load_from_memory(&bytes).context("decoding image")?;
    let (orig_w, orig_h) = img.dimensions();

    // Each terminal row encodes two pixel rows, so the rendered height in
    // pixels is 2 * (rows). We aim for visual square-ish output by halving
    // the vertical scaling.
    let target_w = width.max(2);
    let target_h = ((orig_h as f32) * (target_w as f32 / orig_w as f32) / 2.0).round() as u32;
    let target_h = target_h.max(1) * 2; // must be even
    let resized = img.resize_exact(target_w, target_h, FilterType::Lanczos3).to_rgba8();

    let mut out = String::with_capacity((target_w * target_h) as usize * 20);
    for y in (0..target_h).step_by(2) {
        for x in 0..target_w {
            let top = resized.get_pixel(x, y);
            let bot = resized.get_pixel(x, y + 1);
            write_pixel(&mut out, top, bot)?;
        }
        out.push_str("\x1b[0m\n");
    }
    Ok(out)
}

fn write_pixel(out: &mut String, top: &Rgba<u8>, bot: &Rgba<u8>) -> Result<()> {
    let [tr, tg, tb, ta] = top.0;
    let [br, bg, bb, ba] = bot.0;

    // Treat fully transparent pixels as a reset so backgrounds stay clean.
    if ta == 0 && ba == 0 {
        out.push_str("\x1b[0m ");
        return Ok(());
    }

    write!(
        out,
        "\x1b[38;2;{tr};{tg};{tb}m\x1b[48;2;{br};{bg};{bb}m\u{2580}"
    )?;
    Ok(())
}

fn load_bytes(source: &str) -> Result<Vec<u8>> {
    if source.starts_with("http://") || source.starts_with("https://") {
        let resp = reqwest::blocking::get(source)?.error_for_status()?;
        Ok(resp.bytes()?.to_vec())
    } else {
        let path = Path::new(source);
        std::fs::read(path).with_context(|| format!("reading {}", path.display()))
    }
}

pub fn render_default(source: &str) -> Result<String> {
    render(source, DEFAULT_WIDTH)
}
