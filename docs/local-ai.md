# Optional local generative AI (Ollama)
The public GitHub Pages demo remains offline and uses the deterministic FleetAdvisor, not a large language model.
A separate optional terminal companion uses an already-installed Ollama model on 127.0.0.1:11434. It explains a fixed synthetic aggregate scenario only, without access to files, actual fleet records, the database, credentials or write operations. No API key, paid API or automatic model download is used. Install Ollama separately, choose a suitable local model and run:
```sh
ollama list
python tools/local_ai.py --model YOUR_INSTALLED_MODEL --topic maintenance --language ru
```
Topics: overview, maintenance, dispatch. If Ollama is absent or returns an invalid response, the CLI exits with an error. The public browser is not connected to Ollama. Model output can hallucinate; the printed input counts are the reference, never the generated answer. This tool does not change records or certify vehicle safety. CI tests a mocked loopback service, not real model quality. Model weights and their licenses are independent of this repository. Do not expose Ollama to the internet.
