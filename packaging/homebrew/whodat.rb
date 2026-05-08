class Whodat < Formula
  desc "global, public registry of identities — claim a handle, get looked up from any terminal"
  homepage "https://github.com/HueByte/whodat"
  version "0.2.0"
  license "MIT"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/HueByte/whodat/releases/download/v0.2.0/whodat-aarch64-apple-darwin.tar.gz"
      sha256 "f82dc3ee7060ac0b62b992b7e9b5c3e662de126e4b44178015d857b91b2b8a01"
    else
      url "https://github.com/HueByte/whodat/releases/download/v0.2.0/whodat-x86_64-apple-darwin.tar.gz"
      sha256 "3f2999df017c01f0d673b9e1b3082bc656f4147d595457dbbaeb246fdd2454ad"
    end
  end

  on_linux do
    if Hardware::CPU.arm?
      url "https://github.com/HueByte/whodat/releases/download/v0.2.0/whodat-aarch64-unknown-linux-musl.tar.gz"
      sha256 "b9d5e21e55780725f06a3bbb4a3fd1a305a5affc794d5b1dff19b10ec0908933"
    else
      url "https://github.com/HueByte/whodat/releases/download/v0.2.0/whodat-x86_64-unknown-linux-musl.tar.gz"
      sha256 "f902230413c41f84a92c7de81984bab21d31ae89020d151db8a9ab77e3acdfb7"
    end
  end

  def install
    bin.install "whodat"
  end

  test do
    assert_match "whodat", shell_output("#{bin}/whodat --version")
  end
end
