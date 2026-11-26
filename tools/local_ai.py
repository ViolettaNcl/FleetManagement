"""Optional local Ollama adapter. No keys, fleet database, external API or writes."""
import argparse
import json
import re
import sys
import urllib.request
import urllib.error

URL = "http://127.0.0.1:11434/api/chat"
MAX_RESPONSE = 256 * 1024
# A deliberately synthetic scenario, never taken from a user's database.
SCENARIO = {"synthetic": True, "vehicles": 12, "drivers": 10, "trips_in_progress": 5,
            "service_due": 2, "service_soon": 3, "unassigned_trips": 1}
QUESTIONS = {
    "overview": "Summarize the supplied counts and suggest a review order.",
    "maintenance": "Explain why service-due and service-soon counts merit review.",
    "dispatch": "Explain why the unassigned trip must be checked against availability and service constraints."
}
class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None

def explain(model, topic, language):
    if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._:/-]{0,100}", model):
        raise ValueError("Choose an installed local model by its name.")
    system = ("You explain a fictional fleet demo. Use ONLY supplied aggregate counts. "
              "Do not invent records, declare vehicles safe, recommend automatic assignments, "
              "execute commands or imply actual operational data. State that the data is synthetic. "
              "Use " + ("Russian" if language == "ru" else "English") + ". Max 150 words.")
    payload = {"model": model, "stream": False, "messages": [
        {"role": "system", "content": system},
        {"role": "user", "content": QUESTIONS[topic] + "\n" + json.dumps(SCENARIO)}
    ], "options": {"temperature": 0.1, "num_predict": 320}}
    request = urllib.request.Request(URL, data=json.dumps(payload).encode("utf-8"),
                                     headers={"Content-Type": "application/json"}, method="POST")
    # Do not honor system proxies, credentials, or redirects.
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())
    with opener.open(request, timeout=60) as response:
        data = response.read(MAX_RESPONSE + 1)
    if len(data) > MAX_RESPONSE:
        raise ValueError("Local response exceeded limit.")
    result = json.loads(data)
    text = result["message"]["content"]
    if not isinstance(text, str) or not text.strip() or len(text) > 12000:
        raise ValueError("Local model returned an invalid response.")
    # Strip control characters before terminal display.
    return "".join(c for c in text if c in "\n\t" or (c.isprintable() and c != "\x1b"))

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--model", required=True, help="Name of an already installed local Ollama model")
    parser.add_argument("--topic", choices=QUESTIONS, default="overview")
    parser.add_argument("--language", choices=("en", "ru"), default="en")
    args = parser.parse_args()
    try:
        text = explain(args.model, args.topic, args.language)
    except (OSError, ValueError, KeyError, TypeError) as exc:
        print("Local model unavailable or invalid response: " + type(exc).__name__, file=sys.stderr)
        return 1
    print("SYNTHETIC DEMO — UNVERIFIED GENERATED TEXT\n" + json.dumps(SCENARIO) + "\n")
    print(text)
    return 0
if __name__ == "__main__":
    sys.exit(main())
