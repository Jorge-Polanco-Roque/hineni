"""Builds the rigged, clothed, posed characters from Quaternius' Universal Base Characters (CC0).
Usage: Blender -b -P Tools/build_characters.py -- <Universal Base Characters[Standard] dir> <out dir> [preview dir]

Each character is exported as a skinned FBX whose rest pose is the designed pose, so Unity can add
procedural motion on top (head, breathing, tremor). Faces get blend shapes:
  Parpadeo (blink), Miedo (eyes wide, brows up, pinpoint pupils), Grito (jaw drop, mouth opens).
Feet at y = 0, facing Unity +Z."""
import bpy, bmesh, math, os, sys
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

ARGS = sys.argv[sys.argv.index("--") + 1:]
SRC, OUT = ARGS[:2]
PREVIEW = ARGS[2] if len(ARGS) > 2 else None
TORSO = {"spine_01", "spine_02", "spine_03", "clavicle_l", "clavicle_r"}
LEGS = {"thigh_l", "thigh_r", "calf_l", "calf_r"}
ARMS = ("upperarm_l", "upperarm_r")
FEET = ("foot_l", "foot_r", "ball_l", "ball_r")


# ------------------------------------------------------------------ helpers
def material(name, color, rough=.85):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = rough
    return m


def assign(o, mat):
    o.data.materials.clear(); o.data.materials.append(mat)


def imported(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    return [o for o in bpy.data.objects if o not in before]


def dominant_bone(obj, v):
    best = max(v.groups, key=lambda g: g.weight, default=None)
    return obj.vertex_groups[best.group].name if best else ""


def active(o):
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True); bpy.context.view_layer.objects.active = o


def apply_all(o):
    active(o)
    for m in list(o.modifiers):
        bpy.ops.object.modifier_apply(modifier=m.name)


def rig_to_head(o, arm):
    """Rigid parts (hair, pupils, mouth): skinned 100 % to the head bone."""
    mw = o.matrix_world.copy()
    o.parent = arm; o.parent_type = "OBJECT"; o.matrix_world = mw
    vg = o.vertex_groups.get("Head") or o.vertex_groups.new(name="Head")
    vg.add(range(len(o.data.vertices)), 1, "REPLACE")
    o.modifiers.new("Armature", "ARMATURE").object = arm


def smoothstep(x):
    x = max(0., min(1., x)); return x * x * (3 - 2 * x)


# ------------------------------------------------------------------ building blocks
def load_body(kind):
    objs = imported(f"{SRC}/Base Characters/Unity/Superhero_{kind}_FullBody.fbx")
    arm = next(o for o in objs if o.type == "ARMATURE")
    meshes = [o for o in objs if o.type == "MESH"]
    body = next(o for o in meshes if o.name.lower().startswith("superhero"))
    eyes = next(o for o in meshes if o.name.startswith("Eyes"))
    brows = next(o for o in meshes if o.name.startswith("Eyebrows"))
    return arm, body, eyes, brows


def add_hair(arm, body, name, head_scale=1.0):
    """Hair is modelled for another head size: seat it on this skull's crown, then bind it to the head bone."""
    objs = [o for o in imported(f"{SRC}/Hairstyles/Origin at 0/FBX (Unity)/{name}.fbx") if o.type == "MESH"]
    skull = [body.matrix_world @ v.co for v in body.data.vertices if dominant_bone(body, v) == "Head"]
    crown = max(p.z for p in skull)
    centre = sum(skull, Vector()) / len(skull)
    for o in objs:
        o.matrix_world = Matrix.Translation(centre) @ Matrix.Scale(head_scale, 4) @ Matrix.Translation(-centre) @ o.matrix_world
        # The hair FBX faces the opposite way to the (rotated) body armature: turn it around the skull.
        o.matrix_world = Matrix.Translation((centre.x, centre.y, 0)) @ Matrix.Rotation(math.pi, 4, "Z") \
            @ Matrix.Translation((-centre.x, -centre.y, 0)) @ o.matrix_world
        bpy.context.view_layer.update()
        if not name.startswith("Hair_Beard"):
            top = max((o.matrix_world @ v.co).z for v in o.data.vertices)
            o.matrix_world = Matrix.Translation((0, 0, crown + .006 - top)) @ o.matrix_world
    bpy.context.view_layer.update()
    for o in objs:
        mw = o.matrix_world.copy()
        o.parent, o.parent_type, o.parent_bone = arm, "BONE", "Head"
        o.matrix_world = mw
    return objs


def segment(arm, bone):
    b = arm.data.bones[bone]
    return arm.matrix_world @ b.head_local, arm.matrix_world @ b.tail_local


def proportion(arm, body, eyes, brows, p):
    """Bring the heroic base bodies to everyday proportions, in the rest pose, before clothes are made.
    Every change is blended by the vertex's skin weights, so regions meet without seams."""
    limbs = {bone: k for bone, k in p["limbs"].items()}
    spine_y = segment(arm, "spine_03")[0].y
    head_a, head_b = segment(arm, "Head")
    head_c = head_a + (head_b - head_a) * .35
    groups = {g.index: g.name for g in body.vertex_groups}
    for v in body.data.vertices:
        w = body.matrix_world @ v.co
        move = Vector()
        for g in v.groups:
            name = groups.get(g.group, ""); wt = g.weight
            if name in limbs:   # thinner limbs: pull towards the bone axis
                a, b = segment(arm, name); ab = b - a
                t = max(0, min(1, (w - a).dot(ab) / ab.length_squared)); q = a + ab * t
                move += (q + (w - q) * limbs[name] - w) * wt
            elif name in ("spine_02", "spine_03"):   # less chest depth / bust, a little narrower below the shoulders
                d = Vector((w.x * (p["chest_x"] - 1) if w.z < p["shoulder_z"] else 0, (w.y - spine_y) * (p["chest_y"] - 1) if w.y > spine_y else 0, 0))
                move += d * wt
            elif name in ("pelvis", "thigh_l", "thigh_r") and w.z > .8:
                move += Vector((w.x * (p["hips"] - 1), 0, 0)) * wt
            elif name == "Head":
                move += (head_c + (w - head_c) * p["head"] - w) * wt
        v.co = body.matrix_world.inverted() @ (w + move)
    for o in (eyes, brows):
        for v in o.data.vertices:
            w = o.matrix_world @ v.co
            v.co = o.matrix_world.inverted() @ (head_c + (w - head_c) * p["head"])


MALE = dict(limbs={"upperarm_l": .8, "upperarm_r": .8, "lowerarm_l": .84, "lowerarm_r": .84, "thigh_l": .88, "thigh_r": .88,
                   "calf_l": .9, "calf_r": .9, "neck_01": .84}, chest_x=.93, chest_y=.86, shoulder_z=1.4, hips=1.0, head=1.09)
FEMALE = dict(limbs={"upperarm_l": .9, "upperarm_r": .9, "lowerarm_l": .92, "lowerarm_r": .92, "thigh_l": .88, "thigh_r": .88,
                     "calf_l": .93, "calf_r": .93, "neck_01": .94}, chest_x=.96, chest_y=.72, shoulder_z=1.35, hips=.93, head=1.06)
SHOULDERS = [("clavicle_l", "Y", -8), ("clavicle_r", "Y", 8)]   # heroic square shoulders, lowered


def garment(body, name, keep, offset, mat, smooth=10):
    """A shell of the body limited to `keep(bone, rest_position)`, smoothed so anatomy reads as cloth."""
    g = body.copy(); g.data = body.data.copy(); g.name = name
    bpy.context.collection.objects.link(g)
    bm = bmesh.new(); bm.from_mesh(g.data); bm.verts.ensure_lookup_table()
    drop = [bm.verts[v.index] for v in g.data.vertices if not keep(dominant_bone(g, v), v.co)]
    bmesh.ops.delete(bm, geom=drop, context="VERTS")
    bm.to_mesh(g.data); bm.free()
    assign(g, mat)
    arm_mod = next(m for m in g.modifiers if m.type == "ARMATURE")
    sm = g.modifiers.new("Tela", "SMOOTH"); sm.factor, sm.iterations = .7, smooth
    d = g.modifiers.new("Holgura", "DISPLACE"); d.strength, d.mid_level = offset, 0
    so = g.modifiers.new("Grosor", "SOLIDIFY"); so.thickness, so.offset = .006, 1
    for m in (sm, d, so):   # cloth modifiers act on the rest shape; the armature must run last
        active(g); bpy.ops.object.modifier_move_to_index(modifier=m.name, index=list(g.modifiers).index(arm_mod))
    return g


def tuck(body, covered, depth=.012):
    """Pull skin that clothing covers slightly inwards so it never pokes through."""
    vg = body.vertex_groups.new(name="Cubierto")
    vg.add([v.index for v in body.data.vertices if covered(dominant_bone(body, v), v.co)], 1, "REPLACE")
    d = body.modifiers.new("Bajo la ropa", "DISPLACE"); d.strength, d.mid_level, d.vertex_group = -depth, 0, "Cubierto"
    active(body); bpy.ops.object.modifier_move_to_index(modifier=d.name, index=0)


def turn(arm, bone, axis, degrees):
    """Rotate a pose bone around a world axis through its head (character faces +Y while posing)."""
    bpy.context.view_layer.update()
    pb = arm.pose.bones[bone]
    local = arm.matrix_world.inverted().to_3x3() @ Vector({"X": (1, 0, 0), "Y": (0, 1, 0), "Z": (0, 0, 1)}[axis])
    r = Matrix.Rotation(math.radians(degrees), 4, local)
    pb.matrix = Matrix.Translation(pb.head) @ r @ Matrix.Translation(-pb.head) @ pb.matrix


def pose_as_rest(arm, meshes, steps):
    """Pose the skeleton, bake the pose into every mesh and make it the new rest pose."""
    active(arm); bpy.ops.object.mode_set(mode="POSE")
    for bone, axis, deg in steps:
        turn(arm, bone, axis, deg)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.context.view_layer.update()
    rigid = []
    for o in meshes:
        if o.parent_type == "BONE":   # hair: freeze it where the posed head put it
            mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
            active(o); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
            rigid.append(o)
        else:
            apply_all(o)
    active(arm); bpy.ops.object.mode_set(mode="POSE")
    bpy.ops.pose.armature_apply(selected=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    for o in meshes:
        if o in rigid:
            rig_to_head(o, arm)
        else:
            o.modifiers.new("Armature", "ARMATURE").object = arm
            if o.parent != arm:
                mw = o.matrix_world.copy(); o.parent = arm; o.matrix_world = mw


# ------------------------------------------------------------------ face
def face_frame(arm, eyes):
    ev = [eyes.matrix_world @ v.co for v in eyes.data.vertices]
    mid = sum(ev, Vector()) / len(ev)
    b = arm.data.bones["Head"]
    head = arm.matrix_world @ b.head_local; tail = arm.matrix_world @ b.tail_local
    up = (tail - head).normalized()
    # Split the eyes by the side axis of the head bone.
    side_guess = (arm.matrix_world.to_3x3() @ b.matrix_local.to_3x3() @ Vector((1, 0, 0))).normalized()
    left = [v for v in ev if (v - mid).dot(side_guess) < 0]; right = [v for v in ev if (v - mid).dot(side_guess) >= 0]
    cl, cr = sum(left, Vector()) / len(left), sum(right, Vector()) / len(right)
    side = (cr - cl).normalized()
    fwd = up.cross(side).normalized()
    if fwd.dot(mid - head) < 0: fwd = -fwd
    r = max((v - cl).length for v in left)
    return dict(cl=cl, cr=cr, mid=mid, up=up, side=side, fwd=fwd, r=r)


def mouth_point(body, f):
    """Find the lip line: the deepest point of the face profile between nose and chin."""
    dg = bpy.context.evaluated_depsgraph_get()
    bvh = BVHTree.FromObject(body, dg)
    inv = body.matrix_world.inverted(); mw = body.matrix_world
    profile = []
    for i in range(60):
        k = .025 + i * .0012
        origin = f["mid"] - f["up"] * k + f["fwd"] * .25
        hit = bvh.ray_cast(inv @ origin, (inv.to_3x3() @ -f["fwd"]).normalized())
        if hit[0]: profile.append((k, (mw @ hit[0] - f["mid"]).dot(f["fwd"]), mw @ hit[0]))
    nose = max(profile, key=lambda p: p[1])
    below = [p for p in profile if nose[0] + .008 < p[0] < nose[0] + .06]
    peaks = [below[i] for i in range(1, len(below) - 1) if below[i][1] >= below[i - 1][1] and below[i][1] >= below[i + 1][1]]
    if len(peaks) >= 2:   # upper and lower lip: the mouth line is the valley between them
        between = [p for p in below if peaks[0][0] < p[0] < peaks[1][0]]
        if between: return min(between, key=lambda p: p[1])[2]
    return min(below, key=lambda p: abs(p[0] - (nose[0] + .022)))[2]


def disk(name, center, normal, up, w, h, mat, segments=20):
    bm = bmesh.new()
    side = up.cross(normal).normalized()
    c = bm.verts.new(center)
    ring = [bm.verts.new(center + side * math.cos(a) * w + up * math.sin(a) * h)
            for a in (2 * math.pi * i / segments for i in range(segments))]
    for i in range(segments): bm.faces.new((c, ring[i], ring[(i + 1) % segments]))
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o)
    o.data.materials.append(mat)
    return o


def keys(o, *names):
    if not o.data.shape_keys: o.shape_key_add(name="Basis")
    return [o.shape_key_add(name=n) for n in names]


def set_key(o, key, fn):
    """fn(world_position) -> new world position, for every vertex."""
    inv = o.matrix_world.inverted()
    for v, kv in zip(o.data.vertices, key.data):
        kv.co = inv @ fn(o.matrix_world @ v.co)


def build_face(arm, body, eyes, brows, mats):
    f = face_frame(arm, eyes)
    r, up, fwd, side = f["r"], f["up"], f["fwd"], f["side"]
    mouth = mouth_point(body, f)
    every = ("Parpadeo", "Miedo", "Grito")
    # Pupils: dark disks on the front of each eye; fear shrinks them to points, blink tucks them away.
    for c in (f["cl"], f["cr"]):
        front = c + fwd * r * 1.02
        p = disk("Pupila", front, fwd, up, r * .3, r * .3, mats["pupila"], 12)
        rig_to_head(p, arm)
        blink, fear, _ = keys(p, *every)
        set_key(p, fear, lambda w: front + fwd * r * .03 + (w - front) * .35)
        set_key(p, blink, lambda w: c + (w - c) - up * (w - c).dot(up) * .95 - fwd * r * .5)
    # Mouth: a dark cavity on the lip line. Closed it reads as a hard line, open it is a scream.
    m = disk("Boca", mouth + fwd * .003, fwd, up, .018, .0012, mats["boca"], 24)
    rig_to_head(m, arm)
    _, _, scream = keys(m, *every)
    set_key(m, scream, lambda w: mouth + fwd * .006 + side * (w - mouth).dot(side) * .85
            + up * ((w - mouth - fwd * .003).dot(up) * 14 - .008))
    # Eyeballs: blink flattens them into the socket, fear widens them.
    blink, fear, _ = keys(eyes, *every)
    near = lambda w: f["cl"] if (w - f["mid"]).dot(side) < 0 else f["cr"]
    set_key(eyes, blink, lambda w: near(w) + (w - near(w)) - up * (w - near(w)).dot(up) * .92 - fwd * r * .45)
    set_key(eyes, fear, lambda w: near(w) + (w - near(w)) * 1.2 + fwd * r * .15)
    # Brows lift with fear, inner ends most (the classic grief/fear brow).
    _, fear, _ = keys(brows, *every)
    set_key(brows, fear, lambda w: w + up * r * (.5 + .45 * (1 - min(1, abs((w - f["mid"]).dot(side)) / (r * 3)))))
    # Skin: lids, brow ridge and jaw.
    blink, fear, scream = keys(body, *every)
    inv = body.matrix_world.inverted()
    for v, kb, kf, kg in zip(body.data.vertices, blink.data, fear.data, scream.data):
        w = body.matrix_world @ v.co
        if (w - f["mid"]).length > .15: continue
        b_off = f_off = Vector()
        for c in (f["cl"], f["cr"]):
            d = w - c; lat = abs(d.dot(side)); du = d.dot(up)
            if lat < r * 1.5 and d.dot(fwd) > -r * .6:
                if 0 < du < r * 1.9:
                    lid = (1 - smoothstep(lat / (r * 1.5))) * (1 - smoothstep(du / (r * 1.9)))
                    b_off = b_off - up * du * .9 * lid + fwd * r * .25 * lid
                    f_off = f_off + up * r * .45 * lid
                if r * 1.2 < du < r * 3.4:
                    brow = (1 - smoothstep(lat / (r * 1.6))) * (1 - smoothstep(abs(du - r * 2.2) / (r * 1.2)))
                    f_off = f_off + up * r * .5 * brow
        kb.co = inv @ (w + b_off); kf.co = inv @ (w + f_off)
        dm = w - mouth; du = dm.dot(up); lat = abs(dm.dot(side))
        if du < .004 and lat < .055 and dm.dot(fwd) > -.05:
            jaw = (1 - smoothstep(lat / .055)) * smoothstep(-du / .012) * (1 - smoothstep(-du / .09))
            kg.co = inv @ (w - up * .028 * jaw - fwd * .006 * jaw)


# ------------------------------------------------------------------ assembly / export
def finish(arm, name):
    meshes = [o for o in bpy.data.objects if o.type == "MESH" and o.parent == arm]
    body = next(o for o in meshes if o.name.lower().startswith("superhero"))
    low = min((body.matrix_world @ v.co).z for v in body.data.vertices)   # feet, not whatever cloth hangs lowest
    arm.location.z -= low
    arm.rotation_euler.z += math.pi   # face -Y in Blender = +Z in Unity
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    for o in meshes: o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + ".fbx"), use_selection=True,
                             object_types={"ARMATURE", "MESH"}, add_leaf_bones=False, bake_anim=False,
                             use_armature_deform_only=True, mesh_smooth_type="FACE",
                             apply_scale_options="FBX_SCALE_ALL", path_mode="STRIP")
    print("EXPORTED", name, sum(len(o.data.polygons) for o in meshes), "faces")
    if PREVIEW: preview(meshes, name)


def preview(objs, name):
    scene = bpy.context.scene
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); scene.collection.objects.link(cam)
    scene.camera = cam
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"; scene.display.shading.color_type = "MATERIAL"
    scene.render.resolution_x, scene.render.resolution_y = 420, 560
    bpy.context.view_layer.update()
    head = max(objs, key=lambda o: 0)  # any object; face height from the highest eye vertex below
    top = max((o.matrix_world @ v.co).z for o in objs for v in o.data.vertices)
    eyes = next(o for o in objs if o.name.startswith("Eyes"))
    eye_z = sum((eyes.matrix_world @ v.co).z for v in eyes.data.vertices) / len(eyes.data.vertices)
    eye_y = sum((eyes.matrix_world @ v.co).y for v in eyes.data.vertices) / len(eyes.data.vertices)
    for label, dist, z, lens, states in (("full", 3.6, top * .52, 50, ("neutro",)), ("face", .55, eye_z - .03, 85, ("neutro", "miedo", "parpadeo"))):
        cam.data.lens = lens
        cam.location = (0, eye_y - dist if label == "face" else -dist, z); cam.rotation_euler = (math.radians(90), 0, 0)
        for state in states:
            for o in objs:
                if o.data.shape_keys:
                    for kb in o.data.shape_keys.key_blocks[1:]:
                        kb.value = 1 if (state == "miedo" and kb.name in ("Miedo", "Grito")) or (state == "parpadeo" and kb.name == "Parpadeo") else 0
            scene.render.filepath = os.path.join(PREVIEW, f"{name}_{label}_{state}.png")
            bpy.ops.render.render(write_still=True)
    for o in objs:
        if o.data.shape_keys:
            for kb in o.data.shape_keys.key_blocks[1:]: kb.value = 0


def character(name, kind, skin, hair, hair_color, clothes, poses, extra=None):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mats = {"pupila": material("Pupila", (.01, .01, .01), .2), "boca": material("Boca", (0, 0, 0), 1)}
    arm, body, eyes, brows = load_body(kind)
    build = MALE if kind == "Male" else FEMALE
    proportion(arm, body, eyes, brows, build)
    assign(body, material("Piel", skin))
    assign(eyes, material("Ojos", (.86, .85, .82), .3))
    assign(brows, material("Cejas", (.04, .035, .03)))
    hair_objs = []
    for h in hair:
        for o in add_hair(arm, body, h, build["head"]): assign(o, material("Pelo", hair_color)); hair_objs.append(o)
    garments, covered = [], []
    for g in clothes:
        garments.append(garment(body, g["name"], g["keep"], g["offset"], material(g["mat"], g["color"]), g.get("smooth", 10)))
        covered.append(g["keep"])
    tuck(body, lambda b, c: b not in FEET and any(k(b, c) for k in covered))
    pose_as_rest(arm, [body, eyes, brows] + garments + hair_objs, SHOULDERS + poses)
    if extra: extra(arm, body)
    build_face(arm, body, eyes, brows, mats)
    finish(arm, name)


# ------------------------------------------------------------------ clothes
def scrubs(color):
    return [
        dict(name="Casaca", mat="Uniforme", color=color, offset=.028, smooth=14,
             keep=lambda b, c: (b in TORSO and c.z < 1.5) or (b in ARMS and abs(c.x) < .33) or (b in ("pelvis", "thigh_l", "thigh_r") and c.z > .74)),
        dict(name="Pantalon", mat="Uniforme", color=color, offset=.02,
             keep=lambda b, c: (b == "pelvis" or b in LEGS) and c.z > .1),
        dict(name="Zuecos", mat="Calzado", color=(.1, .1, .1), offset=.012, smooth=6,
             keep=lambda b, c: b in FEET or (b in ("calf_l", "calf_r") and c.z < .12)),
    ]


def gown(color, length=.6, sleeves=.42):
    return [dict(name="Bata", mat="Bata", color=color, offset=.03, smooth=14,
                 keep=lambda b, c: (b in TORSO and c.z < 1.47) or (b in ARMS and abs(c.x) < sleeves) or b == "pelvis" or (b in ("thigh_l", "thigh_r") and c.z > length))]


def coat():
    return [
        dict(name="Bata medica", mat="Bata medica", color=(.8, .8, .77), offset=.034, smooth=14,
             keep=lambda b, c: (b in TORSO and c.z < 1.5) or (b in ARMS and abs(c.x) < .36) or b == "pelvis"
             or (b in LEGS and c.z > .5)),
        dict(name="Pantalon", mat="Pantalon", color=(.12, .12, .13), offset=.018,
             keep=lambda b, c: (b == "pelvis" or b in LEGS) and c.z > .1),
        dict(name="Zapatos", mat="Calzado", color=(.05, .05, .05), offset=.012, smooth=6,
             keep=lambda b, c: b in FEET or (b in ("calf_l", "calf_r") and c.z < .12)),
    ]


# ------------------------------------------------------------------ poses
STAND_HANDS = [
    ("pelvis", "Z", 4), ("thigh_r", "Y", 3), ("thigh_l", "Y", -2),
    ("upperarm_l", "Y", -74), ("upperarm_r", "Y", 74), ("upperarm_l", "X", 18), ("upperarm_r", "X", 18),
    ("lowerarm_l", "X", 62), ("lowerarm_r", "X", 62), ("lowerarm_l", "Z", -32), ("lowerarm_r", "Z", 32),
    ("spine_02", "X", -4), ("neck_01", "X", -6), ("Head", "X", -6),
]
SIT = [
    ("thigh_l", "X", 86), ("thigh_r", "X", 86), ("thigh_l", "Z", -6), ("thigh_r", "Z", 8),
    ("calf_l", "X", -80), ("calf_r", "X", -92),
    ("spine_01", "X", -12), ("spine_02", "X", -8), ("spine_03", "X", -4),
    ("upperarm_l", "Y", -80), ("upperarm_r", "Y", 80), ("upperarm_l", "X", 8), ("upperarm_r", "X", 8),
    ("lowerarm_l", "X", 48), ("lowerarm_r", "X", 48), ("lowerarm_l", "Z", -22), ("lowerarm_r", "Z", 22),
    ("neck_01", "X", -4), ("Head", "X", -6),
]
SIT_CHAIR = SIT[:6] + [
    ("spine_01", "X", -4),
    ("upperarm_l", "Y", -76), ("upperarm_r", "Y", 76), ("upperarm_l", "X", 12), ("upperarm_r", "X", 12),
    ("lowerarm_l", "X", 70), ("lowerarm_r", "X", 70),
]
LIMP = [
    ("upperarm_l", "Y", -84), ("upperarm_r", "Y", 84), ("upperarm_l", "X", 4), ("upperarm_r", "X", 4),
    ("lowerarm_l", "X", 8), ("lowerarm_r", "X", 12),
    ("spine_02", "X", -6), ("neck_01", "X", -8),
]
REACH = [
    ("upperarm_l", "Y", -55), ("upperarm_r", "Y", 55), ("upperarm_l", "X", 48), ("upperarm_r", "X", 40),
    ("lowerarm_l", "X", 10), ("lowerarm_r", "X", 16),
    ("spine_03", "X", 4),
]


def blanket(arm, body):
    neck = arm.matrix_world @ arm.data.bones["neck_01"].head_local
    bpy.ops.mesh.primitive_grid_add(x_subdivisions=48, y_subdivisions=36, size=1, location=(neck.x, neck.y - .05, neck.z + .02))
    sheet = bpy.context.active_object; sheet.scale = (1.5, .95, 1); bpy.ops.object.transform_apply(scale=True)
    bm = bmesh.new(); bm.from_mesh(sheet.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if Vector((v.co.x, v.co.y)).length < .15], context="VERTS")
    bm.to_mesh(sheet.data); bm.free()
    col = body.modifiers.new("Colision", "COLLISION"); body.collision.thickness_outer = .012; body.collision.cloth_friction = 40
    cl = sheet.modifiers.new("Tela", "CLOTH"); cl.settings.quality = 8; cl.settings.mass = .5
    cl.settings.tension_stiffness = 12; cl.settings.bending_stiffness = 2; cl.collision_settings.distance_min = .01
    cl.point_cache.frame_end = 70
    for fr in range(1, 71): bpy.context.scene.frame_set(fr)
    cs = sheet.modifiers.new("Reposo", "SMOOTH"); cs.factor, cs.iterations = .6, 6   # irons out crumples at the neck
    so = sheet.modifiers.new("Grosor", "SOLIDIFY"); so.thickness = .012
    apply_all(sheet)
    body.modifiers.remove(col)
    bpy.context.scene.frame_set(1)
    sheet.name = "Manta"; assign(sheet, material("Manta", (.3, .28, .25), .95))
    vg = sheet.vertex_groups.new(name="spine_03"); vg.add(range(len(sheet.data.vertices)), 1, "REPLACE")
    mw = sheet.matrix_world.copy(); sheet.parent = arm; sheet.matrix_world = mw
    sheet.modifiers.new("Armature", "ARMATURE").object = arm


os.makedirs(OUT, exist_ok=True)
ONLY = os.environ.get("ONLY")
CAST = {
    "Ines": lambda: character("Ines", "Female", (.62, .5, .42), ["Hair_Buns"], (.05, .045, .04), scrubs((.36, .45, .43)), STAND_HANDS),
    "Paciente": lambda: character("Paciente", "Male", (.56, .47, .41), ["Hair_Buzzed"], (.3, .29, .28), gown((.62, .64, .6)), SIT, extra=blanket),
    "Doctor": lambda: character("Doctor", "Male", (.55, .45, .39), ["Hair_SimpleParted"], (.12, .1, .09), coat(), LIMP),
    "Mujer": lambda: character("Mujer", "Female", (.6, .5, .44), ["Hair_Long"], (.09, .08, .07), gown((.55, .58, .62), .66), SIT_CHAIR),
    "Esquina": lambda: character("Esquina", "Male", (.52, .44, .4), [], (.1, .1, .1), gown((.5, .5, .47), .55, .36), REACH),
}
for n, build in CAST.items():
    if not ONLY or n in ONLY.split(","):
        build()
