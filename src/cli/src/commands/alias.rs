use crate::{api, config, AliasAction};
use anyhow::{bail, Result};

pub fn run(api: &api::Client, action: AliasAction) -> Result<()> {
    let session = config::load()?;
    let me = api.me(&session.token)?;
    let mut aliases = me.aliases.clone();

    match action {
        AliasAction::Add { name } => {
            let n = name.trim().to_lowercase();
            if aliases.iter().any(|a| a == &n) {
                bail!("alias already set");
            }
            if aliases.len() >= 5 {
                bail!("max 5 aliases — remove one first");
            }
            aliases.push(n);
        }
        AliasAction::Rm { name } => {
            let n = name.trim().to_lowercase();
            let before = aliases.len();
            aliases.retain(|a| a != &n);
            if aliases.len() == before {
                bail!("alias not found");
            }
        }
        AliasAction::Clear => {
            aliases.clear();
        }
    }

    let entry = api.update(
        &session.token,
        &api::UpdateRequest {
            aliases: Some(&aliases),
            ..Default::default()
        },
    )?;

    if entry.aliases.is_empty() {
        println!("no aliases");
    } else {
        println!("aliases: {}", entry.aliases.join(", "));
    }
    Ok(())
}
