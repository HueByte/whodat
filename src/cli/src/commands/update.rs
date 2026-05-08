use anyhow::Result;
use self_update::backends::github;
use self_update::cargo_crate_version;

/// Self-update against the latest GitHub Release. Picks the asset matching
/// the host's target triple (e.g. whodat-x86_64-pc-windows-msvc.zip), verifies
/// the download, and replaces the running binary in-place.
pub fn run(check_only: bool) -> Result<()> {
    let current = cargo_crate_version!();

    let updater = github::Update::configure()
        .repo_owner("HueByte")
        .repo_name("whodat")
        .bin_name("whodat")
        .show_download_progress(true)
        .show_output(true)
        .no_confirm(true)
        .current_version(current)
        .build()?;

    if check_only {
        let latest = updater.get_latest_release()?;
        println!("current: {current}");
        println!("latest:  {}", latest.version);
        let newer =
            self_update::version::bump_is_greater(current, &latest.version).unwrap_or(false);
        println!(
            "→ {}",
            if newer {
                "update available — run `whodat update`"
            } else {
                "up to date"
            }
        );
        return Ok(());
    }

    let status = updater.update()?;
    match status {
        self_update::Status::UpToDate(v) => println!("already on {v}"),
        self_update::Status::Updated(v) => println!("updated to {v}"),
    }
    Ok(())
}
