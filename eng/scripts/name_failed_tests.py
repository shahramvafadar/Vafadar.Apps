"""Names the tests that failed in a run of CI as notices of the run.

The log of a job and the files it uploads can be read only by who is signed in; a notice of a run can be read
by everybody. A step that runs after a failure calls this with the folder of the result files (.trx): every
failed test becomes a notice with the beginning of its message and of its trace. Nothing but the names of
tests and what they said is written.

    python3 eng/scripts/name_failed_tests.py TestResults
"""
import glob
import os
import sys
import xml.etree.ElementTree as ET

NAMESPACE = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
# A step shows ten notices of a kind; the tenth names the tests that have none of their own.
OWN_NOTICES = 9


def clean(text):
    """One line that cannot be read as a command of the runner."""
    return ' '.join((text or '').split()).replace('%', '%25').replace('::', ': :')


def failed_tests(folder):
    for path in sorted(glob.glob(os.path.join(folder, '**', '*.trx'), recursive=True)):
        for result in ET.parse(path).getroot().iterfind('.//t:UnitTestResult', NAMESPACE):
            if result.get('outcome') != 'Failed':
                continue
            message = result.find('.//t:Message', NAMESPACE)
            trace = result.find('.//t:StackTrace', NAMESPACE)
            said = clean(message.text if message is not None else '')[:600]
            where = clean(trace.text if trace is not None else '')[:500]
            yield clean(result.get('testName')), said + ' | ' + where


def main():
    failed = list(failed_tests(sys.argv[1] if len(sys.argv) > 1 else 'TestResults'))
    print(f'{len(failed)} failed')
    for name, said in failed[:OWN_NOTICES]:
        print(f'::error::{name[:200]} -- {said}')
    if len(failed) > OWN_NOTICES:
        print('::error::More tests failed: ' + '; '.join(name for name, _ in failed[OWN_NOTICES:])[:1000])


if __name__ == '__main__':
    main()
