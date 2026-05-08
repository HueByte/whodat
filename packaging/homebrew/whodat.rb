class Whodat < Formula
  desc "global, public registry of identities — claim a handle, get looked up from any terminal"
  homepage "https://github.com/HueByte/whodat"
  version "0.2.1"
  license "MIT"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/HueByte/whodat/releases/download/v0.2.1/whodat-aarch64-apple-darwin.tar.gz"
      sha256 "70b4f3a415151eff9733dbdbe8d456c8b374aa84213a732a180e7927d9d7c731"
    else
      url "https://github.com/HueByte/whodat/releases/download/v0.2.1/whodat-x86_64-apple-darwin.tar.gz"
      sha256 "b7ba6933b0e89c203460824ddeb525071cdeccb7b9efe485dc53a76737e23c09"
    end
  end

  on_linux do
    if Hardware::CPU.arm?
      url "https://github.com/HueByte/whodat/releases/download/v0.2.1/whodat-aarch64-unknown-linux-musl.tar.gz"
      sha256 "cde6d1fc023e7a82a4b3310c571021faed45bf500ff1588a1d2944177ba3cbf2"
    else
      url "https://github.com/HueByte/whodat/releases/download/v0.2.1/whodat-x86_64-unknown-linux-musl.tar.gz"
      sha256 "418ab4cdacbd2901d6484cc95ae1710586515fa8ab6e4673c3823c43c6188ede"
    end
  end

  def install
    bin.install "whodat"
  end

  test do
    assert_match "whodat", shell_output("#{bin}/whodat --version")
  end
end
