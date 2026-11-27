"""Check exact committed optimized artwork and source provenance metadata; no network or optional dependencies."""
import hashlib,json,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
WEB=ROOT/'src/FleetManagement.Web/wwwroot'
class ApprovedArtwork(unittest.TestCase):
    def test_hashes(self):
        manifest=json.loads((ROOT/'docs/fleet-approved-assets.json').read_text())
        self.assertEqual(len(manifest),36)
        for name,m in manifest.items():
            with self.subTest(name=name):
                data=(WEB/m['path']).read_bytes()
                self.assertTrue(m['source_archived_not_committed'])
                self.assertRegex(m['source_sha256'],r'^[0-9a-f]{64}$')
                self.assertTrue(m['source_archive_path'].endswith('.png'))
                self.assertGreater(m['source_bytes'],0)
                self.assertEqual(hashlib.sha256(data).hexdigest(),m['sha256'])
                self.assertEqual(data[:4],b'RIFF'); self.assertEqual(data[8:12],b'WEBP')
                self.assertLessEqual(len(data),300*1024)
                if m['alpha']:
                    self.assertIn(b'ALPH',data)
if __name__=='__main__':unittest.main()
