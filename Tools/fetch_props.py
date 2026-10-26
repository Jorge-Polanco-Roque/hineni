"""Downloads the CC0 Poly Haven props used by the hospital into Assets/Resources/Props/<id>/ (FBX + textures, 1k).
Run from the project root: python3 Tools/fetch_props.py"""
import json, os, urllib.request

PROPS = [
    "wheelchair_01", "old_bed_frame", "mounted_fluorescent_lights", "wall_clock",
    "vintage_radio_transceiver", "korean_public_payphone_01", "vintage_crutches_01",
    "WetFloorSign_01", "clipboard", "security_camera_01", "modular_airduct_rectangular_01",
    "plastic_monobloc_chair_01", "drawer_cabinet", "medical_box", "metal_trash_can",
    "steel_frame_shelves_01", "fire_alarm", "side_table_01",
]

UA = {"User-Agent": "SpookyGameJam-props/1.0"}

def fetch(url):
    return urllib.request.urlopen(urllib.request.Request(url, headers=UA))

def get(url, dest):
    os.makedirs(os.path.dirname(dest), exist_ok=True)
    if not os.path.exists(dest):
        with open(dest, "wb") as f:
            f.write(fetch(url).read())

for pid in PROPS:
    files = json.load(fetch(f"https://api.polyhaven.com/files/{pid}"))
    fbx = files["fbx"]["1k"]["fbx"]
    root = os.path.join("Assets", "Resources", "Props", pid)
    get(fbx["url"], os.path.join(root, f"{pid}.fbx"))
    for rel, inc in fbx.get("include", {}).items():
        get(inc["url"], os.path.join(root, rel))
    print("ok", pid)
