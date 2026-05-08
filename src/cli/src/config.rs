use anyhow::{Context, Result};
use serde::{Deserialize, Serialize};
use std::fs;
use std::path::PathBuf;

#[derive(Debug, Serialize, Deserialize)]
pub struct Session {
    pub handle: String,
    pub token: String,
}

fn config_path() -> Result<PathBuf> {
    let dir = dirs::config_dir()
        .context("no config directory")?
        .join("whodat");
    Ok(dir.join("session.json"))
}

pub fn load() -> Result<Session> {
    let path = config_path()?;
    let bytes = fs::read(&path).with_context(|| {
        format!(
            "no session at {} — run `whodat register` first",
            path.display()
        )
    })?;
    Ok(serde_json::from_slice(&bytes)?)
}

pub fn save(session: &Session) -> Result<()> {
    let path = config_path()?;
    if let Some(parent) = path.parent() {
        fs::create_dir_all(parent)?;
    }
    fs::write(&path, serde_json::to_vec_pretty(session)?)?;
    Ok(())
}

pub fn clear() -> Result<()> {
    let path = config_path()?;
    if path.exists() {
        fs::remove_file(path)?;
    }
    Ok(())
}
