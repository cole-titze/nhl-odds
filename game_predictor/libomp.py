"""Make PyTorch share Homebrew's OpenMP runtime on macOS.

lightgbm/xgboost load Homebrew's libomp, while torch wheels bundle their own copy. Two OpenMP
runtimes in one process make lightgbm hang or segfault once torch is loaded, so locally (where
requirements-experimental.txt installs torch for TabM) torch's copy is replaced with a symlink
to Homebrew's. Re-run after any torch reinstall/upgrade:

    python -m game_predictor.libomp
"""

import importlib.util
import os
import sys

_BREW_LIBOMP = ("/opt/homebrew/opt/libomp/lib/libomp.dylib", "/usr/local/opt/libomp/lib/libomp.dylib")
FIX_COMMAND = "python -m game_predictor.libomp"


def _torch_libomp() -> str | None:
    spec = importlib.util.find_spec("torch")
    if spec is None or not spec.submodule_search_locations:
        return None
    return os.path.join(spec.submodule_search_locations[0], "lib", "libomp.dylib")


def _brew_libomp() -> str | None:
    return next((p for p in _BREW_LIBOMP if os.path.exists(p)), None)


def needs_link() -> bool:
    """True on macOS when torch is installed but doesn't use Homebrew's libomp."""
    if sys.platform != "darwin":
        return False
    torch_lib, brew_lib = _torch_libomp(), _brew_libomp()
    if torch_lib is None or brew_lib is None or not os.path.lexists(torch_lib):
        return False
    return os.path.realpath(torch_lib) != os.path.realpath(brew_lib)


def link() -> None:
    torch_lib, brew_lib = _torch_libomp(), _brew_libomp()
    if sys.platform != "darwin" or torch_lib is None:
        print("Nothing to do (not macOS or torch not installed).")
        return
    if brew_lib is None:
        sys.exit("Homebrew libomp not found — run `brew install libomp` first.")
    if not needs_link():
        print(f"OK: {torch_lib} already uses {brew_lib}")
        return
    if not os.path.islink(torch_lib):
        os.replace(torch_lib, torch_lib + ".bundled")
    else:
        os.remove(torch_lib)
    os.symlink(brew_lib, torch_lib)
    print(f"Linked {torch_lib} -> {brew_lib}")


if __name__ == "__main__":
    link()
