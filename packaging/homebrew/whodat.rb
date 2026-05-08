class Whodat < Formula
  desc "global, public registry of identities — claim a handle, get looked up from any terminal"
  homepage "https://github.com/HueByte/whodat"
  version "0.1.0"
  license "MIT"

  # Placeholder formula. The release workflow regenerates this file with real
  # SHA256 sums on every published version. Until the first release lands,
  # `brew install --formula <url>` will fail because no archives exist yet.

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/HueByte/whodat/releases/download/v0.1.0/whodat-aarch64-apple-darwin.tar.gz"
      sha256 "0000000000000000000000000000000000000000000000000000000000000000"
    else
      url "https://github.com/HueByte/whodat/releases/download/v0.1.0/whodat-x86_64-apple-darwin.tar.gz"
      sha256 "0000000000000000000000000000000000000000000000000000000000000000"
    end
  end

  on_linux do
    if Hardware::CPU.arm?
      url "https://github.com/HueByte/whodat/releases/download/v0.1.0/whodat-aarch64-unknown-linux-musl.tar.gz"
      sha256 "0000000000000000000000000000000000000000000000000000000000000000"
    else
      url "https://github.com/HueByte/whodat/releases/download/v0.1.0/whodat-x86_64-unknown-linux-musl.tar.gz"
      sha256 "0000000000000000000000000000000000000000000000000000000000000000"
    end
  end

  def install
    bin.install "whodat"
  end

  test do
    assert_match "whodat", shell_output("#{bin}/whodat --version")
  end
end
