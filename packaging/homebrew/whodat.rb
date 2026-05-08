class Whodat < Formula
  desc "global, public registry of identities — claim a handle, get looked up from any terminal"
  homepage "https://github.com/HueByte/whodat"
  version "0.2.0"
  license "MIT"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/HueByte/whodat/releases/download/v0.2.0/whodat-aarch64-apple-darwin.tar.gz"
      sha256 "7e92e0bc467a33a71e314a49f5cf20ad92eef5e69d04df2ae7e5a94c92130781"
    else
      url "https://github.com/HueByte/whodat/releases/download/v0.2.0/whodat-x86_64-apple-darwin.tar.gz"
      sha256 "4b5127babc2f5994ff3efba1f9f3292ec8a488b59bb6624c9db2797ff10dcd5c"
    end
  end

  on_linux do
    if Hardware::CPU.arm?
      url "https://github.com/HueByte/whodat/releases/download/v0.2.0/whodat-aarch64-unknown-linux-musl.tar.gz"
      sha256 "67c9b69833494d4ce9f3e686663ea486e0eb346a0db1da577474745ba9cb0063"
    else
      url "https://github.com/HueByte/whodat/releases/download/v0.2.0/whodat-x86_64-unknown-linux-musl.tar.gz"
      sha256 "43d7110a046171a978dff6382b9b9abbb89ce748f9a5df69788b310fac958d61"
    end
  end

  def install
    bin.install "whodat"
  end

  test do
    assert_match "whodat", shell_output("#{bin}/whodat --version")
  end
end
