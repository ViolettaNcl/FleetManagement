"""Static source integration contract. Real rendering is tested separately by Playwright."""
import hashlib,json,re,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
WEB=ROOT/'src/FleetManagement.Web'
WWW=WEB/'wwwroot'
class EnterpriseContract(unittest.TestCase):
    def test_module_registry_paths(self):
        manifest=json.loads((ROOT/'docs/fleet-approved-assets.json').read_text(encoding='utf-8'))
        registry=(WEB/'AssetRegistry.cs').read_text(encoding='utf-8')
        keys=re.findall(r'"([a-z-]+)"\s*=>\s*"(assets/[^"\n]+\.webp)"',registry)
        self.assertGreaterEqual(len(keys),20)
        for name,path in keys:
            self.assertEqual(path,manifest[name]['path'])
            self.assertEqual(hashlib.sha256((WWW/path).read_bytes()).hexdigest(),manifest[name]['sha256'])
    def test_workspace_references(self):
        app=(WEB/'App.razor').read_text(encoding='utf-8')
        index=(WWW/'index.html').read_text(encoding='utf-8')
        self.assertIn('data-module="@section"',app)
        self.assertIn('<ModuleHeroBanner',app)
        self.assertIn('css/fleet-enterprise.css',index)
        self.assertIn('css/fleet-approved.css',index)
        for path in re.findall(r'(?:src|href)="(assets/[^"\n]+)"',index):
            self.assertTrue((WWW/path).is_file(),path)
        for name in ('fleet-approved.css','fleet-enterprise.css'):
            css=(WWW/'css'/name).read_text(encoding='utf-8')
            for path in re.findall(r'url\("\.\./(assets/[^"\n]+)"\)',css):self.assertTrue((WWW/path).is_file(),path)
    def test_public_art_mapping(self):
        import sys
        sys.path.insert(0,str(ROOT/'tools'))
        from build_public_site import PUBLIC_ART,PAGES
        self.assertEqual(set(PUBLIC_ART),{p[0] for p in PAGES})
        for path in PUBLIC_ART.values():self.assertTrue((WWW/path.removeprefix('app/')).is_file(),path)
if __name__=='__main__':unittest.main()
