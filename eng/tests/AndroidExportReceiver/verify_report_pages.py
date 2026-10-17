"""Inspect the original English D-312 report and rendered footer visibility, including continuation context."""
import argparse
import json
from pathlib import Path
import subprocess
import tempfile
from PIL import Image
from pypdf import PdfReader


def require(condition, message):
    """Reject missing native evidence rather than accepting PDF text hidden by clipping."""
    if not condition:
        raise ValueError(message)


def main():
    """Render every original page and check the known fictitious report, without rebuilding its calculations."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("pdf", type=Path)
    args = parser.parse_args()
    reader = PdfReader(args.pdf)
    require(len(reader.pages) == 2, "Expected the two-page original fixture")
    texts = [page.extract_text() for page in reader.pages]
    require("D312_Fictitious_Cash" in texts[1] and "Accounts in totals" in texts[1]
            and "Item" in texts[1] and "Amount" in texts[1], "Continued account context is missing")
    combined = " ".join("\n".join(texts).split())
    for value in ["1,000.00 EUR", "21.00 EUR", "979.00 EUR", "879.00 EUR", "100.00 EUR", "5.50 EUR", "26.50 EUR", "D318_First_Plan"]:
        require(value in combined, "Known original figure/name missing: " + value)
    ink_counts = []
    with tempfile.TemporaryDirectory(prefix="zanance-report-render-") as temporary:
        prefix = str(Path(temporary) / "page")
        subprocess.run(["pdftoppm", "-scale-to", "1400", "-png", str(args.pdf.resolve()), prefix], check=True)
        for index, page in enumerate(reader.pages, start=1):
            require("This is not an official bank statement or tax document." in texts[index - 1], "Disclaimer text missing")
            with Image.open(prefix + "-" + str(index) + ".png") as image:
                # Body rows end above 776pt. This band belongs only to the footer, not a table border.
                xscale = image.width / float(page.mediabox.width)
                yscale = image.height / float(page.mediabox.height)
                footer = image.convert("L").crop((int(36 * xscale), int(785 * yscale),
                                                int(559 * xscale), int(806 * yscale)))
                ink = sum(count for shade, count in enumerate(footer.histogram()) if shade < 245)
                require(ink > 40, "Footer is present as text but visually clipped on page " + str(index))
                ink_counts.append(ink)
    print(json.dumps({"passed": True, "pages": len(texts), "visibleFooterPixels": ink_counts}, indent=2))


if __name__ == "__main__":
    main()
