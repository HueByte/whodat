use crate::{api, ascii, config, render};
use anyhow::Result;
use std::collections::BTreeMap;

pub fn run(
    api: &api::Client,
    text: Option<String>,
    avatar: Option<String>,
    meta: Vec<(String, String)>,
) -> Result<()> {
    let session = config::load()?;
    let avatar_ascii = avatar.as_deref().map(ascii::render_default).transpose()?;
    let metadata: BTreeMap<String, String> = meta.into_iter().collect();

    let entry = api.update(
        &session.token,
        &api::UpdateRequest {
            text: text.as_deref(),
            avatar_ascii: avatar_ascii.as_deref(),
            metadata: if metadata.is_empty() { None } else { Some(&metadata) },
        },
    )?;

    render::entry(&entry);
    Ok(())
}
