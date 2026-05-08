use crate::{api, config};
use anyhow::{bail, Result};
use std::time::Duration;

pub fn run(api: &api::Client, github: bool) -> Result<()> {
    if !github {
        bail!("only --github login is supported");
    }

    let start = api.github_start(None)?;

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
            handle: None,
            text: None,
            avatar_ascii: None,
            metadata: None,
        };
        match api.github_complete(&req)? {
            api::GithubPoll::Done(t) => {
                config::save(&config::Session {
                    handle: t.handle.clone(),
                    token: t.token,
                })?;
                println!("logged in as {}", t.handle);
                return Ok(());
            }
            api::GithubPoll::Pending { slow_down } => {
                if slow_down {
                    interval = interval.saturating_add(5);
                }
            }
        }
    }
}
