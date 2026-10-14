Built netstandard2.1 DLLs of the HtmlToUitk core are copied here by unity/tools/copy-plugins.sh (or .ps1):
HtmlToUitk.Contracts, HtmlToUitk.Core, HtmlToUitk.Capabilities.Model, HtmlToUitk.Rules, HtmlToUitk.Convert, HtmlToUitk.Bake, HtmlToUitk.Preview.
The script also writes the Editor-only .meta files with stable GUIDs.
Without these DLLs the window runs the job through the htmltouitk CLI process instead.
