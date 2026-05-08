use crate::api::Entry;
use chrono_compat::format_unix;
use owo_colors::OwoColorize;

/// Visible width of the avatar block — must match `ascii::DEFAULT_WIDTH`.
const AVATAR_WIDTH: usize = 40;

/// Width of the right column when an avatar is present. Tuned so the whole
/// composition fits in an 88-col terminal: 2 indent + 40 avatar + 4 sep + 40 right + 2 = 88.
const RIGHT_WIDTH: usize = 40;

pub fn entry(e: &Entry) {
    println!();

    let avatar_lines: Vec<String> = e
        .avatar_ascii
        .as_deref()
        .map(|a| a.lines().map(String::from).collect())
        .unwrap_or_default();

    let header = format!(
        "{} {}",
        e.handle.bold(),
        format!("(registered {})", format_unix(e.registered_at)).dimmed()
    );

    let mut right_lines: Vec<String> = vec![header];
    if !e.aliases.is_empty() {
        right_lines.push(format!("{} {}", "also:".dimmed(), e.aliases.join(", ")));
    }
    let mut flags: Vec<String> = Vec::new();
    if e.is_hidden {
        flags.push("hidden".yellow().to_string());
    }
    if !e.random_visible && !e.is_hidden {
        flags.push("undiscoverable".dimmed().to_string());
    }
    if !flags.is_empty() {
        right_lines.push(format!("[ {} ]", flags.join(" / ")));
    }
    if let Some(text) = &e.text {
        right_lines.push(String::new());
        for line in textwrap::wrap(text, RIGHT_WIDTH) {
            right_lines.push(line.into_owned());
        }
    }

    if avatar_lines.is_empty() {
        for line in &right_lines {
            println!("  {line}");
        }
    } else {
        let blank = " ".repeat(AVATAR_WIDTH);
        let height = avatar_lines.len().max(right_lines.len());
        for i in 0..height {
            let left = avatar_lines.get(i).unwrap_or(&blank);
            let right = right_lines.get(i).map(String::as_str).unwrap_or("");
            println!("  {left}    {right}");
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
