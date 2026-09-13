import unittest
from pathlib import Path


ROOT = Path(__file__).parents[1]


class ProductSeparationTests(unittest.TestCase):
    def test_cpw_installer_does_not_manage_pwpanel(self):
        installer = (ROOT / "installer" / "install-asp-cpw.sh").read_text(encoding="utf-8")
        self.assertNotIn("/opt/pw155-web", installer)
        self.assertNotIn("pw155-web.service", installer)
        self.assertIn("asp-cpw-http.service", installer)
        self.assertIn("groupadd --system aspcpw", installer)
        self.assertIn("install -d -o root -g aspcpw -m 0770", installer)
        self.assertIn("usermod -a -G aspcpw pwweb", installer)

    def test_default_patch_url_uses_standalone_service(self):
        settings = (ROOT / "desktop" / "src" / "AppSettings.cs").read_text(encoding="utf-8")
        self.assertIn("http://127.0.0.1:8082/patch/", settings)


if __name__ == "__main__":
    unittest.main()
