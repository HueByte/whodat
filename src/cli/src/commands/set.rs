use crate::{api, ascii, config, profile, render};
use anyhow::Result;
use std::path::PathBuf;

pub fn run(
    api: &api::Client,
    profile_path: Option<PathBuf>,
    text: Option<String>,
    avatar: Option<String>,
    meta: Vec<(String, String)>,
) -> Result<()> {
    let session = config::load()?;

    let loaded = match profile_path {
        Some(p) => profile::Profile::load(&p)?,
        None => profile::Profile::default(),
    };
    let (text, avatar, metadata) = profile::merge(loaded, text, avatar, meta);

    let avatar_ascii = avatar.as_deref().map(ascii::render_default).transpose()?;

    let entry = api.update(
        &session.token,
        &api::UpdateRequest {
            text: text.as_deref(),
            avatar_ascii: avatar_ascii.as_deref(),
            metadata: if metadata.is_empty() {
                None
            } else {
                Some(&metadata)
            },
        },
    )?;

    render::entry(&entry);
    Ok(())
}
