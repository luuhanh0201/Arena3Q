import zipfile, os, sys, json
src = sys.argv[1]; dst = sys.argv[2]
m = {"SF01":"ancient_chinese_house","SF02":"chines_house_-_low_poly","SF03":"chinese_house","SF04":"low_poly_asian_house",
"SF05":"asian_house_pack_-_low_poly","SF06":"chinese_roof._low_poly_model.","SF07":"chinese_style_tea__pavilion",
"SF08":"chinese_pavilion_memoriam","SF09":"ancient_chinese_arena","SF11":"chinese_drum_tower","SF12":"low_poly_watchtower_isometric",
"SF13":"lowp_-_lunar_year_-_cc0_asset_pack","SF15":"free_bamboo_set","SF16":"bamboo_bush","SF17":"bamboo",
"SF18":"sakura_tree_01_-_low_poly_model","SF19":"japanese_cherry_tree_low-poly","SF20":"rice_field",
"SF21":"chinese_incense_burner-lowpory-material_test","SF23":"old_chinese_lantern_-_lowpoly_for_free_download",
"SF24":"chinese_lantern","SF26":"low_bridge"}
for code,name in m.items():
    zp = os.path.join(src, name + ".zip")
    if not os.path.exists(zp): print(code, "MISSING ZIP"); continue
    out = os.path.realpath(os.path.join(dst, code))
    with zipfile.ZipFile(zp) as z:
        bad = [n for n in z.namelist() if not os.path.realpath(os.path.join(out, n)).startswith(out + os.sep)]
        if bad: print(code, "UNSAFE PATHS", bad[:3]); continue
        if z.testzip(): print(code, "CORRUPT"); continue
        os.makedirs(out, exist_ok=True); z.extractall(out)
        names = z.namelist()
        print(code, name, len(names), "files;", sorted({os.path.splitext(n)[1].lower() for n in names if not n.endswith('/')}))
