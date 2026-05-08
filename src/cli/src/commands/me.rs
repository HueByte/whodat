use crate::{api::Client, config, render};
use anyhow::Result;

pub fn run(api: &Client) -> Result<()> {
    let session = config::load()?;
    let entry = api.me(&session.token)?;
    render::entry(&entry);
    Ok(())
}
