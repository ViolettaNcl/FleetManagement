import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest
import zipfile
spec=importlib.util.spec_from_file_location("asset_import",Path(__file__).resolve().parents[2]/"tools/import_neon_assets.py")
module=importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

class ArtworkImport(unittest.TestCase):
    def test_modes(self):
        with tempfile.TemporaryDirectory() as root:
            root=Path(root)
            out=root/"publish"
            (out/"css").mkdir(parents=True)
            (out/"index.html").write_text("test")
            source=root/"design"
            source.mkdir()
            self.assertEqual(module.install(out,source)["mode"],"fallback")
            self.assertNotIn("url(",(out/"css/neon-assets.css").read_text())
            archive=source/"fixture.zip"
            data=b"RIFF"+bytes(4)+b"WEBP"+bytes(8)
            with zipfile.ZipFile(archive,"w") as z:
                for stem in module.SLOTS.values():
                    z.writestr("webp/"+stem+".webp",data)
                z.writestr("../outside.txt","never extracted")
            with self.assertRaisesRegex(ValueError,"Unrecognized"):
                module.install(out,source)
            old=module.ORIGINAL
            try:
                module.ORIGINAL=hashlib.sha256(archive.read_bytes()).hexdigest()
                result=module.install(out,source)
            finally:
                module.ORIGINAL=old
            self.assertEqual(result["mode"],"imported")
            self.assertEqual(len(result["files"]),8)
            self.assertFalse((root/"outside.txt").exists())
            self.assertEqual((out/"assets/neon/hero.webp").read_bytes(),data)
            (source/"second.zip").write_bytes(b"")
            with self.assertRaisesRegex(ValueError,"exactly one"):
                module.install(out,source)
            archive.unlink()
            (source/"second.zip").unlink()
            self.assertEqual(module.install(out,source)["mode"],"fallback")
            self.assertFalse((out/"assets/neon/hero.webp").exists())

if __name__=="__main__":
    unittest.main()
