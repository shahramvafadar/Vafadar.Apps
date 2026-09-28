"""Unit tests for the structural checker, not logo artwork or rendering tests."""
from pathlib import Path
import tempfile
import unittest
from check_svg_structure import inspect_svg

START = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">'


class SvgPolicyTests(unittest.TestCase):
    def check(self, body: str, flat: bool = False) -> dict:
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'fixture.svg'
            path.write_text(START + body + '</svg>', encoding='utf-8')
            return inspect_svg(path, flat=flat)

    def test_solid_shape(self):
        self.assertTrue(self.check('<path d="M0 0L10 0L5 10Z" fill="#000000"/>', True)['structural_pass'])

    def test_vector_gradient(self):
        body = ('<defs><linearGradient id="a"><stop offset="0" stop-color="#000000"/>'
                '<stop offset="1" stop-color="#ffffff"/></linearGradient></defs>'
                '<path d="M0 0L10 0L5 10Z" fill="url(#a)"/>')
        self.assertTrue(self.check(body)['structural_pass'])
        self.assertFalse(self.check(body, True)['structural_pass'])

    def test_wrapped_bitmap(self):
        self.assertFalse(self.check('<image href="data:image/png;base64,AA=="/>')['structural_pass'])

    def test_external_use(self):
        self.assertFalse(self.check('<use href="https://example.invalid/icon.svg#x"/>')['structural_pass'])

    def test_missing_reference(self):
        self.assertFalse(self.check('<path d="M0 0L10 0L5 10Z" fill="url(#missing)"/>')['structural_pass'])

    def test_active_content(self):
        self.assertFalse(self.check('<path d="M0 0L10 0L5 10Z" onclick="alert(1)"/>')['structural_pass'])

    def test_two_color_not_flat(self):
        self.assertFalse(self.check('<rect width="10" height="10" fill="#000000"/>'
                                    '<circle cx="5" cy="5" r="2" fill="#ffffff"/>', True)['structural_pass'])

    def test_font_dependent_text(self):
        self.assertFalse(self.check('<text x="0" y="20">Zanance</text>')['structural_pass'])

    def test_partial_alpha_not_flat(self):
        self.assertFalse(self.check('<path d="M0 0L10 0L5 10Z" fill="#000000" opacity="0.5"/>', True)['structural_pass'])

    def test_doctype_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'doctype.svg'
            path.write_text('<!DOCTYPE svg>' + START + '</svg>', encoding='utf-8')
            self.assertFalse(inspect_svg(path)['structural_pass'])


if __name__ == '__main__':
    unittest.main()
