use crate::{api::Client, render};
use anyhow::Result;

pub fn run(api: &Client, handle: &str) -> Result<()> {
    let entry = api.lookup(handle)?;
    render::entry(&entry);
    Ok(())
}
