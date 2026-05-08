use anyhow::{Context, Result};
use serde::Deserialize;
use std::collections::BTreeMap;
use std::path::Path;

/// JSON profile file. Lets users keep their handle's blurb / avatar / metadata
/// in one file and pass it via `--profile`. Explicit per-flag CLI args still
/// override the corresponding file values.
#[derive(Debug, Default, Deserialize)]
pub struct Profile {
    pub text: Option<String>,
    pub avatar: Option<String>,
    #[serde(default)]
    pub metadata: BTreeMap<String, String>,
}

impl Profile {
    pub fn load(path: &Path) -> Result<Self> {
        let bytes = std::fs::read(path).with_context(|| format!("reading {}", path.display()))?;
        serde_json::from_slice(&bytes).with_context(|| format!("parsing {}", path.display()))
    }
}

/// Merge a profile (read from disk) with per-flag overrides. Explicit CLI
/// flags always win; metadata keys are unioned with explicit keys overriding.
pub fn merge(
    profile: Profile,
    text: Option<String>,
    avatar: Option<String>,
    meta: Vec<(String, String)>,
) -> (Option<String>, Option<String>, BTreeMap<String, String>) {
    let merged_text = text.or(profile.text);
    let merged_avatar = avatar.or(profile.avatar);
    let mut merged_meta = profile.metadata;
    for (k, v) in meta {
        merged_meta.insert(k, v);
    }
    (merged_text, merged_avatar, merged_meta)
}
