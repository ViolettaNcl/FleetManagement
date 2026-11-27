"""Regression tests for the conservative CodeQL fallback."""
import unittest
from release_security import evaluate_analyses

SHA="a"*40
REF="refs/heads/preview/fleetops-v3"
START="2026-11-27T18:42:00Z"
CLEAN={"commit_sha":SHA,"ref":REF,"tool":{"name":"CodeQL"},
       "analysis_key":".github/workflows/codeql.yml:analyze",
       "category":".github/workflows/codeql.yml:analyze/language:csharp",
       "created_at":"2026-11-27T18:44:30Z","id":123,"sarif_id":"test-sarif",
       "results_count":0,"rules_count":59,"error":"","warning":""}

class CodeQLFallback(unittest.TestCase):
    def evaluate(self,rows):
        return evaluate_analyses(rows,SHA,REF,START)["passed"]
    def test_exact_clean(self):
        self.assertTrue(self.evaluate([CLEAN]))
    def test_missing(self):
        self.assertFalse(self.evaluate([]))
    def test_bad_metadata(self):
        for changes in ({"commit_sha":"b"*40},{"ref":"refs/heads/master"},
                        {"tool":{"name":"Other"}},{"analysis_key":"other"},
                        {"results_count":1},{"results_count":None},{"results_count":False},
                        {"rules_count":0},{"error":"error"},{"warning":"warning"},
                        {"warning":None},{"sarif_id":None},{"category":None},
                        {"created_at":"2026-11-26T18:44:30Z"}):
            with self.subTest(changes=changes):
                self.assertFalse(self.evaluate([dict(CLEAN,**changes)]))
    def test_newer_failure_not_hidden(self):
        self.assertFalse(self.evaluate([CLEAN,dict(CLEAN,id=124,results_count=2)]))
    def test_multiple_categories_fail(self):
        self.assertFalse(self.evaluate([CLEAN,dict(CLEAN,id=124,category="other")]))
if __name__=="__main__":
    unittest.main()
