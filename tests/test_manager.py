import importlib.util
import os
import shutil
import sys
import tempfile
import types
import unittest
from pathlib import Path


if "fcntl" not in sys.modules:
    try:
        import fcntl  # noqa: F401
    except ImportError:
        sys.modules["fcntl"] = types.SimpleNamespace(
            LOCK_EX=1, LOCK_NB=2, flock=lambda *_args: None)


class ManagerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory()
        root = Path(cls.temp.name)
        os.environ["ASP_CPW_HOME"] = str(root / "home")
        os.environ["ASP_CPW_DATA"] = str(root / "data")
        os.environ["ASP_CPW_STATE"] = str(root / "state")
        source = Path(__file__).parents[1] / "scripts" / "asp_cpw_manager.py"
        spec = importlib.util.spec_from_file_location("asp_cpw_manager_test", source)
        cls.manager = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(cls.manager)

    @classmethod
    def tearDownClass(cls):
        cls.temp.cleanup()

    def setUp(self):
        for root in (self.manager.STAGING, self.manager.WORK):
            if root.exists():
                shutil.rmtree(root)
        for kind in self.manager.TYPES:
            (self.manager.STAGING / kind).mkdir(parents=True, exist_ok=True)
            (self.manager.WORK / "new" / kind).mkdir(parents=True, exist_ok=True)

    def test_preview_lists_only_regular_staged_files(self):
        source = self.manager.STAGING / "element" / "data" / "elements.data"
        source.parent.mkdir(parents=True, exist_ok=True)
        source.write_bytes(b"data")
        hidden = self.manager.STAGING / "element" / ".secret"
        hidden.write_bytes(b"no")
        result = self.manager.preview()
        self.assertEqual(result["files"]["element"], ["data/elements.data"])
        self.assertEqual(result["total"], 1)

    def test_release_name_rejects_path_traversal(self):
        with self.assertRaises(self.manager.ManagerError):
            self.manager.safe_release("../escape")

    def test_clear_work_input_does_not_touch_staging(self):
        work = self.manager.WORK / "new" / "launcher" / "Launcher.exe"
        work.write_bytes(b"work")
        staged = self.manager.STAGING / "launcher" / "Launcher.exe"
        staged.write_bytes(b"staged")
        self.manager.clear_work_input()
        self.assertFalse(work.exists())
        self.assertEqual(staged.read_bytes(), b"staged")


if __name__ == "__main__":
    unittest.main()
