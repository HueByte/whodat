mod api;
mod ascii;
mod commands;
mod config;
mod profile;
mod render;

use clap::{Parser, Subcommand};
use std::path::PathBuf;

#[derive(Parser)]
#[command(
    name = "whodat",
    version,
    about = "a global, public registry of identities"
)]
struct Cli {
    #[command(subcommand)]
    command: Option<Command>,

    /// Handle to look up (shorthand for `whodat lookup <handle>`).
    handle: Option<String>,

    /// Override the API base URL (defaults to $WHODAT_API or the public registry).
    #[arg(long, global = true)]
    api: Option<String>,
}

#[derive(Subcommand)]
enum Command {
    /// Look up a handle.
    Lookup { handle: String },

    /// Register a new handle.
    Register {
        handle: String,

        /// Use GitHub OAuth instead of a password.
        #[arg(long)]
        github: bool,

        /// Load text/avatar/metadata from a JSON file. Per-flag values override file values.
        #[arg(long)]
        profile: Option<PathBuf>,

        /// Free-text blurb (max 280 chars).
        #[arg(long)]
        text: Option<String>,

        /// Path or URL to an image; converted to colored ASCII before upload.
        #[arg(long)]
        avatar: Option<String>,

        /// Repeatable key=value metadata, e.g. --meta email=me@example.com.
        #[arg(long = "meta", value_parser = parse_kv)]
        meta: Vec<(String, String)>,
    },

    /// Log in on this machine using GitHub OAuth (requires an existing handle).
    Login {
        /// Use GitHub OAuth (currently the only login method).
        #[arg(long, default_value_t = true)]
        github: bool,
    },

    /// Update your existing entry.
    Set {
        /// Load text/avatar/metadata from a JSON file. Per-flag values override file values.
        #[arg(long)]
        profile: Option<PathBuf>,
        #[arg(long)]
        text: Option<String>,
        #[arg(long)]
        avatar: Option<String>,
        #[arg(long = "meta", value_parser = parse_kv)]
        meta: Vec<(String, String)>,
    },

    /// Show your own entry.
    Me,

    /// Delete your registration.
    Delete {
        /// Skip the confirmation prompt.
        #[arg(long)]
        yes: bool,
    },
}

fn parse_kv(s: &str) -> Result<(String, String), String> {
    let (k, v) = s
        .split_once('=')
        .ok_or_else(|| format!("expected key=value, got `{s}`"))?;
    Ok((k.trim().to_string(), v.to_string()))
}

fn main() -> anyhow::Result<()> {
    let cli = Cli::parse();
    let api = api::Client::from_env(cli.api.as_deref())?;

    match (cli.command, cli.handle) {
        (Some(Command::Lookup { handle }), _) | (None, Some(handle)) => {
            commands::lookup::run(&api, &handle)
        }
        (
            Some(Command::Register {
                handle,
                github,
                profile,
                text,
                avatar,
                meta,
            }),
            _,
        ) => commands::register::run(&api, &handle, github, profile, text, avatar, meta),
        (Some(Command::Login { github }), _) => commands::login::run(&api, github),
        (
            Some(Command::Set {
                profile,
                text,
                avatar,
                meta,
            }),
            _,
        ) => commands::set::run(&api, profile, text, avatar, meta),
        (Some(Command::Me), _) => commands::me::run(&api),
        (Some(Command::Delete { yes }), _) => commands::delete::run(&api, yes),
        (None, None) => {
            <Cli as clap::CommandFactory>::command().print_help()?;
            Ok(())
        }
    }
}
