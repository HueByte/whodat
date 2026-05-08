use crate::api::Entry;
use chrono_compat::format_unix;
use owo_colors::OwoColorize;

pub fn entry(e: &Entry) {
    println!();
    if let Some(art) = &e.avatar_ascii {
        for line in art.lines() {
            println!("  {line}");
        }
        println!();
    }

    println!(
        "  {} {}",
        e.handle.bold(),
        format!("(registered {})", format_unix(e.registered_at)).dimmed()
    );

    if let Some(text) = &e.text {
        println!();
        for line in text.lines() {
            println!("  {line}");
        }
    }

    if !e.metadata.is_empty() {
        println!();
        let key_width = e.metadata.keys().map(|k| k.len()).max().unwrap_or(0);
        for (k, v) in &e.metadata {
            println!("  {:<width$}  {}", k.cyan(), v, width = key_width);
        }
    }
    println!();
}

mod chrono_compat {
    /// Tiny stdlib-only formatter so we don't pull in the `chrono` crate
    /// just for a pretty date. Outputs `YYYY-MM-DD` from a unix timestamp.
    pub fn format_unix(ts: i64) -> String {
        let days_since_epoch = ts.div_euclid(86_400);
        let (y, m, d) = civil_from_days(days_since_epoch);
        format!("{y:04}-{m:02}-{d:02}")
    }

    // Howard Hinnant's days-from-civil algorithm, inverted.
    fn civil_from_days(z: i64) -> (i32, u32, u32) {
        let z = z + 719_468;
        let era = if z >= 0 { z } else { z - 146_096 } / 146_097;
        let doe = (z - era * 146_097) as u32;
        let yoe = (doe - doe / 1460 + doe / 36_524 - doe / 146_096) / 365;
        let y = yoe as i64 + era * 400;
        let doy = doe - (365 * yoe + yoe / 4 - yoe / 100);
        let mp = (5 * doy + 2) / 153;
        let d = doy - (153 * mp + 2) / 5 + 1;
        let m = if mp < 10 { mp + 3 } else { mp - 9 };
        let y = if m <= 2 { y + 1 } else { y };
        (y as i32, m, d)
    }
}
