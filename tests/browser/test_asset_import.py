import importlib.util
from pathlib import Path
import shutil
import tempfile
import unittest
from test_approved_assets import ApprovedArtwork
spec = importlib.util.spec_from_file_location("asset_import", Path(__file__).resolve().parents[2]/"tools/import_neon_assets.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

class ArtworkImport(unittest.TestCase):
    def test_actual_assets_and_fail_closed(self):
        source = Path(__file__).resolve().parents[2]/"src/FleetManagement.Web/wwwroot"
        with tempfile.TemporaryDirectory() as root:
            out = Path(root)
            (out/"css").mkdir()
            (out/"index.html").write_text("test")
            shutil.copy(source/"css/neon-assets.css",out/"css/neon-assets.css")
            shutil.copytree(source/"assets/neon",out/"assets/neon")
            result = module.install(out)
            self.assertEqual(result["mode"], "committed")
            self.assertEqual(len(result["files"]), 10)
            image = out/"assets/neon/hero.webp"
            original = image.read_bytes()
            image.write_bytes(original[:-1]+bytes([original[-1]^1]))
            with self.assertRaisesRegex(ValueError, "integrity mismatch"):
                module.install(out)
            image.unlink()
            with self.assertRaisesRegex(ValueError, "Missing"):
                module.install(out)
if __name__=="__main__":
    unittest.main()
