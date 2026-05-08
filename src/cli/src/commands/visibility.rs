use crate::{api, config, render};
use anyhow::Result;

pub fn hide(api: &api::Client) -> Result<()> {
    let entry = flip(api, Some(true), None)?;
    render::entry(&entry);
    Ok(())
}

pub fn unhide(api: &api::Client) -> Result<()> {
    let entry = flip(api, Some(false), None)?;
    render::entry(&entry);
    Ok(())
}

pub fn discoverable(api: &api::Client, on: bool) -> Result<()> {
    let entry = flip(api, None, Some(on))?;
    render::entry(&entry);
    Ok(())
}

fn flip(
    api: &api::Client,
    is_hidden: Option<bool>,
    random_visible: Option<bool>,
) -> Result<api::Entry> {
    let session = config::load()?;
    api.update(
        &session.token,
        &api::UpdateRequest {
            is_hidden,
            random_visible,
            ..Default::default()
        },
    )
}
