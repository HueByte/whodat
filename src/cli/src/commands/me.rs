use crate::{api::Client, config, render};
use anyhow::Result;

pub fn run(api: &Client) -> Result<()> {
    let session = config::load()?;
    let entry = api.lookup(&session.handle)?;
    render::entry(&entry);
    Ok(())
}
