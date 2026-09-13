import importlib.util
import os
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).parents[1] / "scripts" / "asp_cpw_http.py"


class PatchHttpServiceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        (self.root / "info").mkdir()
        (self.root / "element").mkdir()
        (self.root / "info" / "pid").write_text("101", encoding="ascii")
        (self.root / "element" / "version").write_text("1", encoding="ascii")
        os.environ["ASP_CPW_PUBLIC_ROOT"] = str(self.root)
        spec = importlib.util.spec_from_file_location("asp_cpw_http_test", SCRIPT)
        self.service = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(self.service)

    def tearDown(self):
        os.environ.pop("ASP_CPW_PUBLIC_ROOT", None)
        self.temp.cleanup()

    def test_resolves_only_published_channels(self):
        self.assertEqual(self.service.resolve_patch("info/pid"), self.root / "info" / "pid")
        self.assertEqual(self.service.resolve_patch("element/version"), self.root / "element" / "version")
        self.assertIsNone(self.service.resolve_patch("unknown/file"))

    def test_blocks_traversal_and_directories(self):
        self.assertIsNone(self.service.resolve_patch("../secret"))
        self.assertIsNone(self.service.resolve_patch("element/%2e%2e/info/pid"))
        self.assertIsNone(self.service.resolve_patch("element"))


if __name__ == "__main__":
    unittest.main()
