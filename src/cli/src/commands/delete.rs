use crate::{api::Client, config};
use anyhow::{bail, Result};
use std::io::{self, Write};

pub fn run(api: &Client, yes: bool) -> Result<()> {
    let session = config::load()?;
    if !yes {
        print!("delete @{} permanently? [y/N] ", session.handle);
        io::stdout().flush()?;
        let mut answer = String::new();
        io::stdin().read_line(&mut answer)?;
        if !matches!(answer.trim().to_lowercase().as_str(), "y" | "yes") {
            bail!("aborted");
        }
    }

    api.delete(&session.token)?;
    config::clear()?;
    println!("deleted");
    Ok(())
}
