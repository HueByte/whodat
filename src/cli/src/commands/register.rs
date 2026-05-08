use crate::{api, ascii, config};
use anyhow::{bail, Result};
use std::collections::BTreeMap;
use std::time::Duration;

pub fn run(
    api: &api::Client,
    handle: &str,
    github: bool,
    text: Option<String>,
    avatar: Option<String>,
    meta: Vec<(String, String)>,
) -> Result<()> {
    let avatar_ascii = avatar.as_deref().map(ascii::render_default).transpose()?;
    let metadata: BTreeMap<String, String> = meta.into_iter().collect();
    let metadata_ref = if metadata.is_empty() { None } else { Some(&metadata) };

    let resp = if github {
        register_github(api, handle, text.as_deref(), avatar_ascii.as_deref(), metadata_ref)?
    } else {
        register_password(api, handle, text.as_deref(), avatar_ascii.as_deref(), metadata_ref)?
    };

    config::save(&config::Session {
        handle: resp.handle.clone(),
        token: resp.token,
    })?;
    println!("registered {}", resp.handle);
    Ok(())
}

fn register_password(
    api: &api::Client,
    handle: &str,
    text: Option<&str>,
    avatar_ascii: Option<&str>,
    metadata: Option<&BTreeMap<String, String>>,
) -> Result<api::TokenResponse> {
    let password = rpassword::prompt_password("password: ")?;
    let confirm = rpassword::prompt_password("confirm:  ")?;
    if password != confirm {
        bail!("passwords did not match");
    }

    api.register(&api::RegisterRequest {
        handle,
        password: Some(&password),
        text,
        avatar_ascii,
        metadata,
    })
}

fn register_github(
    api: &api::Client,
    handle: &str,
    text: Option<&str>,
    avatar_ascii: Option<&str>,
    metadata: Option<&BTreeMap<String, String>>,
) -> Result<api::TokenResponse> {
    let start = api.github_start(Some(handle))?;

    println!();
    println!("  open:  {}", start.verification_uri);
    println!("  code:  {}", start.user_code);
    println!();
    let _ = webbrowser::open(&start.verification_uri);
    println!("waiting for github authorization (Ctrl+C to cancel)...");

    let mut interval = start.interval.max(1) as u64;
    loop {
        std::thread::sleep(Duration::from_secs(interval));

        let req = api::GithubCompleteRequest {
            device_code: &start.device_code,
            handle: Some(handle),
            text,
            avatar_ascii,
            metadata,
        };
        match api.github_complete(&req)? {
            api::GithubPoll::Done(t) => return Ok(t),
            api::GithubPoll::Pending { slow_down } => {
                if slow_down {
                    interval = interval.saturating_add(5);
                }
            }
        }
    }
}
