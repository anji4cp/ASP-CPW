import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).parents[1]
SCRIPT = ROOT / "tools" / "client-setup" / "set-launcher-links.ps1"


class LauncherLinkTests(unittest.TestCase):
    @unittest.skipUnless(shutil.which("powershell"), "Windows PowerShell is required")
    def test_updates_all_launcher_links_and_preserves_utf16(self):
        source = """<SkinApp>
<SkinButton Name="Regist"><Command Event="click" Name="BrowserLink" URL="old-register"/></SkinButton>
<SkinButton Name="HomePage"><Command Event="click" Name="BrowserLink" URL="old-home"/></SkinButton>
<SkinButton Name="Service"><Command Event="click" Name="BrowserLink" URL="old-support"/></SkinButton>
<SkinButton Name="BBS"><Command Event="click" Name="BrowserLink" URL="old-forum"/></SkinButton>
<SkinBrowser Name="UpdateBrowser" InitURL="old-news"/>
</SkinApp>"""
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "mainuni.xml"
            path.write_text(source, encoding="utf-16")
            command = [
                "powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(SCRIPT),
                "-MainUniPath", str(path), "-NewsUrl", "https://site.test/news?a=1&b=2",
                "-RegisterUrl", "https://site.test/#register", "-HomeUrl", "https://site.test/",
                "-SupportUrl", "https://site.test/support", "-ForumUrl", "https://site.test/forum",
                "-NoBackup",
            ]
            subprocess.run(command, check=True, capture_output=True, text=True)
            data = path.read_bytes()
            self.assertTrue(data.startswith(b"\xff\xfe"))
            text = path.read_text(encoding="utf-16")
            self.assertIn("https://site.test/news?a=1&amp;b=2", text)
            self.assertIn("https://site.test/#register", text)
            self.assertIn("https://site.test/", text)
            self.assertIn("https://site.test/support", text)
            self.assertIn("https://site.test/forum", text)


if __name__ == "__main__":
    unittest.main()
