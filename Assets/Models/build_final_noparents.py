"""
SafeDriver VR - Mini City FINAL - NO PARENTS
Every mesh has origin at its geometry center.
No Empties, no parenting. Each object has correct location.
Blender: X=right, Y=depth, Z=up. FBX converts to Unity: X=right, Y=up, Z=depth(inverted).
"""
import bpy, math, os

EXPORT = r"F:\Git\SafeDriver\Assets\Models\MiniCity_Complete.fbx"

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()
for b in [bpy.data.meshes, bpy.data.materials]:
    for x in list(b):
        if x.users == 0: b.remove(x)

def mat(name, col, emit=False, s=5):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = col
    if emit:
        b.inputs["Emission Color"].default_value = col
        b.inputs["Emission Strength"].default_value = s
    return m

def box(name, loc, sc, m):
    bpy.ops.mesh.primitive_cube_add(location=loc, scale=sc)
    o = bpy.context.active_object; o.name = name
    bpy.ops.object.transform_apply(scale=True)
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='MEDIAN')
    o.data.materials.clear(); o.data.materials.append(m)
    return o

def cyl(name, loc, r, h, m):
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=r, depth=h, location=loc)
    o = bpy.context.active_object; o.name = name
    bpy.ops.object.transform_apply()
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='MEDIAN')
    o.data.materials.clear(); o.data.materials.append(m)
    return o

def sph(name, loc, r, m):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=4, radius=r, location=loc)
    o = bpy.context.active_object; o.name = name
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='MEDIAN')
    o.data.materials.clear(); o.data.materials.append(m)
    return o

def whl(name, loc, axis, m):
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.05, depth=0.03, location=loc)
    o = bpy.context.active_object; o.name = name
    if axis == 'x': o.rotation_euler.x = math.radians(90)
    else: o.rotation_euler.y = math.radians(90)
    bpy.ops.object.transform_apply(rotation=True)
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='MEDIAN')
    o.data.materials.clear(); o.data.materials.append(m)
    return o

# Materials
M = {}
M['road'] = mat("Road", (0.15, 0.15, 0.17, 1))
M['cw'] = mat("CW", (0.95, 0.95, 0.95, 1))
M['line'] = mat("Line", (0.9, 0.85, 0.2, 1))
M['bb'] = mat("BB", (0.35, 0.55, 0.78, 1))
M['bp'] = mat("BP", (0.82, 0.45, 0.55, 1))
M['by'] = mat("BY", (0.9, 0.8, 0.4, 1))
M['bg'] = mat("BG", (0.45, 0.72, 0.55, 1))
M['bo'] = mat("BO", (0.9, 0.55, 0.3, 1))
M['bv'] = mat("BV", (0.6, 0.4, 0.75, 1))
M['win'] = mat("Win", (0.65, 0.82, 0.95, 1))
M['roof'] = mat("Roof", (0.45, 0.28, 0.22, 1))
M['pole'] = mat("Pole", (0.3, 0.3, 0.32, 1))
M['tlh'] = mat("TLH", (0.15, 0.15, 0.17, 1))
M['ron'] = mat("LightRedOn", (1, 0.1, 0.1, 1), True, 8)
M['roff'] = mat("LightRedOff", (0.3, 0.08, 0.08, 1))
M['yon'] = mat("LightYelOn", (1, 0.85, 0.1, 1), True, 8)
M['yoff'] = mat("LightYelOff", (0.3, 0.25, 0.05, 1))
M['gon'] = mat("LightGrnOn", (0.1, 1, 0.2, 1), True, 8)
M['goff'] = mat("LightGrnOff", (0.05, 0.25, 0.08, 1))
M['cb'] = mat("CarBlue", (0.2, 0.4, 0.8, 1))
M['cr'] = mat("CarRed", (0.8, 0.15, 0.15, 1))
M['cwin'] = mat("CarWin", (0.5, 0.65, 0.8, 1))
M['tire'] = mat("Tire", (0.1, 0.1, 0.1, 1))
M['hl'] = mat("HL", (1, 0.95, 0.7, 1), True, 2)
M['skin'] = mat("Skin", (0.85, 0.7, 0.55, 1))
M['sb'] = mat("SB", (0.3, 0.45, 0.75, 1))
M['sr'] = mat("SR", (0.75, 0.2, 0.2, 1))
M['pant'] = mat("Pant", (0.2, 0.2, 0.35, 1))
M['trunk'] = mat("Trunk", (0.4, 0.25, 0.15, 1))
M['leaf'] = mat("Leaf", (0.25, 0.6, 0.2, 1))
M['leaf2'] = mat("Leaf2", (0.15, 0.5, 0.15, 1))

# ── GROUND (NO PARENTS) ──
box("Road_NS", (0, 0, 0.005), (0.7, 4.0, 0.005), M['road'])
box("Road_EW", (0, 0, 0.005), (4.0, 0.7, 0.005), M['road'])
for y in [-2.5, -1.8, -1.2, 1.2, 1.8, 2.5]:
    box(f"LnNS_{y:.0f}", (0, y, 0.012), (0.02, 0.2, 0.002), M['line'])
for x in [-2.5, -1.8, -1.2, 1.2, 1.8, 2.5]:
    box(f"LnEW_{x:.0f}", (x, 0, 0.012), (0.2, 0.02, 0.002), M['line'])
for j in range(5):
    o = (j - 2) * 0.25
    box(f"CwN_{j}", (o, 0.95, 0.012), (0.08, 0.12, 0.002), M['cw'])
    box(f"CwS_{j}", (o, -0.95, 0.012), (0.08, 0.12, 0.002), M['cw'])
    box(f"CwE_{j}", (0.95, o, 0.012), (0.12, 0.08, 0.002), M['cw'])
    box(f"CwW_{j}", (-0.95, o, 0.012), (0.12, 0.08, 0.002), M['cw'])

# ── BUILDINGS ──
def bld(name, x, y, w, d, h, bm):
    box(f"{name}_Body", (x, y, h/2), (w/2, d/2, h/2), bm)
    box(f"{name}_Roof", (x, y, h+0.04), (w/2+0.04, d/2+0.04, 0.04), M['roof'])
    for f in range(int(h/0.55)):
        wz = 0.35 + f * 0.55
        box(f"{name}_WF{f}", (x, y+d/2+0.01, wz), (0.07, 0.005, 0.08), M['win'])
        box(f"{name}_WB{f}", (x, y-d/2-0.01, wz), (0.07, 0.005, 0.08), M['win'])

bld("BNW1", -2.0, 2.0, 0.9, 0.9, 1.6, M['bb'])
bld("BNW2", -3.0, 2.3, 0.6, 0.6, 1.1, M['by'])
bld("BNE1", 2.0, 2.0, 0.8, 0.8, 2.0, M['bp'])
bld("BNE2", 2.8, 2.8, 0.6, 0.6, 0.9, M['bg'])
bld("BSW1", -2.0, -2.0, 0.9, 0.9, 1.4, M['bo'])
bld("BSW2", -2.9, -2.6, 0.5, 0.5, 1.8, M['bv'])
bld("BSE1", 2.0, -2.0, 1.0, 0.8, 1.5, M['bg'])
bld("BSE2", 2.7, -2.7, 0.6, 0.6, 2.2, M['bb'])

# ── TRAFFIC LIGHTS (NO PARENTS - each piece is independent) ──
def tl(name, x, y, dx, dy):
    cyl(f"{name}_Pole", (x, y, 0.5), 0.035, 1.0, M['pole'])
    box(f"{name}_Housing", (x+dx*0.4, y+dy*0.4, 1.1), (0.06, 0.06, 0.16), M['tlh'])
    sph(f"{name}_Red", (x+dx, y+dy, 1.2), 0.035, M['ron'])
    sph(f"{name}_Yellow", (x+dx, y+dy, 1.1), 0.035, M['yoff'])
    sph(f"{name}_Green", (x+dx, y+dy, 1.0), 0.035, M['goff'])

tl("TrafficLight_North", 0.8, 0.8, 0, -0.06)
tl("TrafficLight_South", -0.8, -0.8, 0, 0.06)
tl("TrafficLight_East", 0.8, -0.8, -0.06, 0)
tl("TrafficLight_West", -0.8, 0.8, 0.06, 0)

# ── CARS (NO PARENTS - each piece is independent) ──
# Car Blue going North at (0.25, -2.5)
cx, cy = 0.25, -2.5
box("Car_Blue_Body", (cx, cy, 0.13), (0.15, 0.3, 0.08), M['cb'])
box("Car_Blue_Cabin", (cx, cy, 0.27), (0.12, 0.16, 0.07), M['cb'])
box("Car_Blue_WS", (cx, cy+0.14, 0.27), (0.10, 0.015, 0.05), M['cwin'])
box("Car_Blue_WR", (cx, cy-0.16, 0.27), (0.10, 0.015, 0.05), M['cwin'])
box("Car_Blue_HL1", (cx+0.07, cy+0.31, 0.14), (0.02, 0.01, 0.02), M['hl'])
box("Car_Blue_HL2", (cx-0.07, cy+0.31, 0.14), (0.02, 0.01, 0.02), M['hl'])
whl("Car_Blue_W0", (cx+0.16, cy+0.18, 0.05), 'y', M['tire'])
whl("Car_Blue_W1", (cx+0.16, cy-0.18, 0.05), 'y', M['tire'])
whl("Car_Blue_W2", (cx-0.16, cy+0.18, 0.05), 'y', M['tire'])
whl("Car_Blue_W3", (cx-0.16, cy-0.18, 0.05), 'y', M['tire'])

# Car Red going West at (2.5, 0.25)
cx, cy = 2.5, 0.25
box("Car_Red_Body", (cx, cy, 0.13), (0.3, 0.15, 0.08), M['cr'])
box("Car_Red_Cabin", (cx, cy, 0.27), (0.16, 0.12, 0.07), M['cr'])
box("Car_Red_WS", (cx-0.14, cy, 0.27), (0.015, 0.10, 0.05), M['cwin'])
box("Car_Red_WR", (cx+0.16, cy, 0.27), (0.015, 0.10, 0.05), M['cwin'])
box("Car_Red_HL1", (cx-0.31, cy+0.07, 0.14), (0.01, 0.02, 0.02), M['hl'])
box("Car_Red_HL2", (cx-0.31, cy-0.07, 0.14), (0.01, 0.02, 0.02), M['hl'])
whl("Car_Red_W0", (cx+0.18, cy+0.16, 0.05), 'x', M['tire'])
whl("Car_Red_W1", (cx+0.18, cy-0.16, 0.05), 'x', M['tire'])
whl("Car_Red_W2", (cx-0.18, cy+0.16, 0.05), 'x', M['tire'])
whl("Car_Red_W3", (cx-0.18, cy-0.16, 0.05), 'x', M['tire'])

# ── PEDESTRIANS ──
def ped(name, x, y, sm):
    sph(f"{name}_Head", (x, y, 0.62), 0.05, M['skin'])
    box(f"{name}_Torso", (x, y, 0.44), (0.06, 0.04, 0.10), sm)
    box(f"{name}_LL", (x, y+0.02, 0.2), (0.03, 0.03, 0.12), M['pant'])
    box(f"{name}_LR", (x, y-0.02, 0.2), (0.03, 0.03, 0.12), M['pant'])

ped("Pedestrian_1", -1.2, 0.95, M['sb'])
ped("Pedestrian_2", 1.2, -0.95, M['sr'])
ped("Pedestrian_3", -0.95, -1.2, M['sb'])
ped("Pedestrian_4", 0.95, 1.2, M['sr'])

# ── TREES ──
def tree(name, x, y, lm):
    cyl(f"{name}_Trunk", (x, y, 0.25), 0.035, 0.5, M['trunk'])
    s = sph(f"{name}_Leaf", (x, y, 0.65), 0.25, lm)
    s.scale = (1, 1, 0.75)
    bpy.context.view_layer.objects.active = s
    bpy.ops.object.transform_apply(scale=True)
    bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='MEDIAN')

tree("Tree_1", -1.6, 1.4, M['leaf'])
tree("Tree_2", 1.6, 1.4, M['leaf2'])
tree("Tree_3", -1.6, -1.4, M['leaf'])
tree("Tree_4", 1.6, -1.4, M['leaf2'])
tree("Tree_5", -3.2, 0, M['leaf'])
tree("Tree_6", 3.2, 0, M['leaf2'])

# ── EXPORT ── NO PARENTS, all flat ──
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(
    filepath=EXPORT,
    use_selection=True,
    apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z',
    axis_up='Y',
    apply_unit_scale=True,
    bake_space_transform=True,
    mesh_smooth_type='FACE',
)
print("DONE - NO PARENTS EXPORT")
