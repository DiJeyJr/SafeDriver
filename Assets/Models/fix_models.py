"""
SafeDriver VR - Fix Models
Corrige semáforos y autos con rotaciones correctas.
Ejecutar en Blender > Scripting > Run Script (Alt+P)
"""

import bpy
import math
import os

EXPORT_DIR = r"F:\Git\SafeDriver\Assets\Models"

def clean_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    for m in list(bpy.data.meshes):
        if m.users == 0: bpy.data.meshes.remove(m)
    for m in list(bpy.data.materials):
        if m.users == 0: bpy.data.materials.remove(m)

def mat(name, color, emission=False, strength=5.0):
    m = bpy.data.materials.new(name=name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    if emission:
        bsdf.inputs["Emission Color"].default_value = color
        bsdf.inputs["Emission Strength"].default_value = strength
    return m

def add_cube(name, loc, scale, material):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    o.data.materials.clear()
    o.data.materials.append(material)
    return o

def add_cylinder(name, loc, radius, depth, material, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=radius, depth=depth, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    bpy.ops.object.transform_apply(rotation=True)
    o.data.materials.clear()
    o.data.materials.append(material)
    return o

def add_sphere(name, loc, radius, material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=4, radius=radius, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.data.materials.clear()
    o.data.materials.append(material)
    return o

def add_plane(name, loc, size, material):
    bpy.ops.mesh.primitive_plane_add(size=size, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.data.materials.clear()
    o.data.materials.append(material)
    return o

# ============================================================
# MATERIALS
# ============================================================
def create_mats():
    m = {}
    m['road'] = mat("Road", (0.15, 0.15, 0.17, 1))
    m['sidewalk'] = mat("Sidewalk", (0.6, 0.58, 0.55, 1))
    m['crosswalk'] = mat("Crosswalk", (0.95, 0.95, 0.95, 1))
    m['road_line'] = mat("RoadLine", (0.9, 0.9, 0.2, 1))
    m['building_blue'] = mat("Bld_Blue", (0.35, 0.55, 0.78, 1))
    m['building_pink'] = mat("Bld_Pink", (0.82, 0.45, 0.55, 1))
    m['building_yellow'] = mat("Bld_Yellow", (0.9, 0.8, 0.4, 1))
    m['building_green'] = mat("Bld_Green", (0.45, 0.72, 0.55, 1))
    m['building_orange'] = mat("Bld_Orange", (0.9, 0.55, 0.3, 1))
    m['building_purple'] = mat("Bld_Purple", (0.6, 0.4, 0.75, 1))
    m['window'] = mat("Window", (0.7, 0.85, 0.95, 1))
    m['roof'] = mat("Roof", (0.5, 0.3, 0.25, 1))
    m['pole'] = mat("Pole", (0.25, 0.25, 0.27, 1))
    m['housing'] = mat("TL_Housing", (0.12, 0.12, 0.14, 1))
    m['red_on'] = mat("Light_Red_On", (1, 0.1, 0.1, 1), True, 8)
    m['red_off'] = mat("Light_Red_Off", (0.3, 0.08, 0.08, 1))
    m['yellow_on'] = mat("Light_Yellow_On", (1, 0.85, 0.1, 1), True, 8)
    m['yellow_off'] = mat("Light_Yellow_Off", (0.3, 0.25, 0.05, 1))
    m['green_on'] = mat("Light_Green_On", (0.1, 1, 0.2, 1), True, 8)
    m['green_off'] = mat("Light_Green_Off", (0.05, 0.25, 0.08, 1))
    m['car_blue'] = mat("Car_Blue", (0.2, 0.4, 0.8, 1))
    m['car_red'] = mat("Car_Red", (0.8, 0.15, 0.15, 1))
    m['car_window'] = mat("CarWindow", (0.5, 0.65, 0.8, 1))
    m['tire'] = mat("Tire", (0.1, 0.1, 0.1, 1))
    m['headlight'] = mat("Headlight", (1, 0.95, 0.7, 1), True, 2)
    m['skin'] = mat("Skin", (0.85, 0.7, 0.55, 1))
    m['shirt_blue'] = mat("Shirt_Blue", (0.3, 0.45, 0.75, 1))
    m['shirt_red'] = mat("Shirt_Red", (0.75, 0.2, 0.2, 1))
    m['pants'] = mat("Pants", (0.2, 0.2, 0.35, 1))
    m['shoes'] = mat("Shoes", (0.15, 0.12, 0.1, 1))
    m['trunk'] = mat("Trunk", (0.4, 0.25, 0.15, 1))
    m['canopy'] = mat("Canopy", (0.25, 0.6, 0.2, 1))
    m['canopy_dk'] = mat("Canopy_Dk", (0.15, 0.45, 0.15, 1))
    return m

# ============================================================
# TRAFFIC LIGHT - Fixed rotation using parent empty
# ============================================================
def build_traffic_light(name, pos, facing_angle, m):
    """Crea semáforo orientado correctamente usando un Empty padre."""
    parts = []

    # Poste vertical
    pole = add_cylinder(f"{name}_Pole", (0, 0.6, 0), 0.04, 1.2, m['pole'])
    parts.append(pole)

    # Carcasa
    housing = add_cube(f"{name}_Housing", (0, 1.3, 0.06), (0.08, 0.18, 0.05), m['housing'])
    parts.append(housing)

    # Luces - mirando hacia afuera (+Z)
    light_r = add_sphere(f"{name}_Red", (0, 1.42, 0.12), 0.04, m['red_on'])
    light_y = add_sphere(f"{name}_Yellow", (0, 1.3, 0.12), 0.04, m['yellow_off'])
    light_g = add_sphere(f"{name}_Green", (0, 1.18, 0.12), 0.04, m['green_off'])
    parts.extend([light_r, light_y, light_g])

    # Crear empty padre para rotar todo junto
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=pos)
    parent = bpy.context.active_object
    parent.name = name
    parent.rotation_euler.z = math.radians(facing_angle)

    for p in parts:
        p.parent = parent

    bpy.ops.object.select_all(action='DESELECT')
    parent.select_set(True)
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parent
    bpy.ops.object.transform_apply(rotation=True)

    return [parent] + parts

# ============================================================
# CAR - Fixed wheel rotation
# ============================================================
def build_car(name, pos, facing_angle, body_mat, m):
    """Crea un auto orientado correctamente."""
    parts = []

    # Carrocería inferior (orientada en +X = frente)
    body = add_cube(f"{name}_BodyLow", (0, 0.15, 0), (0.35, 0.1, 0.17), body_mat)
    parts.append(body)

    # Cabina
    cabin = add_cube(f"{name}_BodyTop", (-0.02, 0.32, 0), (0.18, 0.08, 0.14), body_mat)
    parts.append(cabin)

    # Parabrisas
    windshield = add_cube(f"{name}_Windshield", (0.16, 0.32, 0), (0.02, 0.06, 0.12), m['car_window'])
    parts.append(windshield)

    # Luneta trasera
    rear_win = add_cube(f"{name}_RearWindow", (-0.19, 0.32, 0), (0.02, 0.06, 0.12), m['car_window'])
    parts.append(rear_win)

    # Ruedas - cilindros tumbados en X (rotados en X 90°)
    wheel_pos = [
        (0.2, 0.06, 0.17),   # Delantera derecha
        (0.2, 0.06, -0.17),  # Delantera izquierda
        (-0.2, 0.06, 0.17),  # Trasera derecha
        (-0.2, 0.06, -0.17), # Trasera izquierda
    ]
    for i, wp in enumerate(wheel_pos):
        wheel = add_cylinder(f"{name}_Wheel_{i}", wp, 0.06, 0.04, m['tire'],
                           rot=(math.radians(90), 0, 0))
        parts.append(wheel)

    # Faros
    hl_l = add_cube(f"{name}_HL_L", (0.36, 0.17, 0.09), (0.01, 0.025, 0.025), m['headlight'])
    hl_r = add_cube(f"{name}_HL_R", (0.36, 0.17, -0.09), (0.01, 0.025, 0.025), m['headlight'])
    parts.extend([hl_l, hl_r])

    # Parent empty para orientar
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=pos)
    parent = bpy.context.active_object
    parent.name = name
    parent.rotation_euler.z = math.radians(facing_angle)

    for p in parts:
        p.parent = parent

    bpy.ops.object.select_all(action='DESELECT')
    parent.select_set(True)
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parent
    bpy.ops.object.transform_apply(rotation=True)

    return [parent] + parts

# ============================================================
# PEDESTRIAN
# ============================================================
def build_pedestrian(name, pos, shirt_mat, m):
    parts = []
    head = add_sphere(f"{name}_Head", (0, 0.7, 0), 0.06, m['skin'])
    torso = add_cube(f"{name}_Torso", (0, 0.5, 0), (0.07, 0.12, 0.05), shirt_mat)
    leg_l = add_cube(f"{name}_LegL", (0, 0.22, 0.025), (0.035, 0.15, 0.035), m['pants'])
    leg_r = add_cube(f"{name}_LegR", (0, 0.22, -0.025), (0.035, 0.15, 0.035), m['pants'])
    arm_l = add_cube(f"{name}_ArmL", (0, 0.5, 0.1), (0.025, 0.1, 0.025), m['skin'])
    arm_r = add_cube(f"{name}_ArmR", (0, 0.5, -0.1), (0.025, 0.1, 0.025), m['skin'])
    shoe_l = add_cube(f"{name}_ShoeL", (0.01, 0.04, 0.025), (0.035, 0.02, 0.03), m['shoes'])
    shoe_r = add_cube(f"{name}_ShoeR", (0.01, 0.04, -0.025), (0.035, 0.02, 0.03), m['shoes'])
    parts = [head, torso, leg_l, leg_r, arm_l, arm_r, shoe_l, shoe_r]

    bpy.ops.object.empty_add(type='PLAIN_AXES', location=pos)
    parent = bpy.context.active_object
    parent.name = name
    for p in parts:
        p.parent = parent

    return [parent] + parts

# ============================================================
# TREE
# ============================================================
def build_tree(name, pos, canopy_mat, m):
    trunk = add_cylinder(f"{name}_Trunk", (pos[0], pos[1]+0.3, pos[2]), 0.04, 0.6, m['trunk'])
    canopy = add_sphere(f"{name}_Canopy", (pos[0], pos[1]+0.8, pos[2]), 0.3, canopy_mat)
    canopy.scale.y = 0.7
    bpy.context.view_layer.objects.active = canopy
    bpy.ops.object.transform_apply(scale=True)
    return [trunk, canopy]

# ============================================================
# BUILDING
# ============================================================
def build_building(name, pos, w, h, d, body_mat, m):
    parts = []
    x, y, z = pos
    body = add_cube(f"{name}_Body", (x, y+h/2, z), (w/2, h/2, d/2), body_mat)
    roof = add_cube(f"{name}_Roof", (x, y+h+0.05, z), (w/2+0.05, 0.05, d/2+0.05), m['roof'])
    parts.extend([body, roof])

    floors = int(h / 0.6)
    for fl in range(floors):
        wy = y + 0.4 + fl * 0.6
        win_f = add_cube(f"{name}_WF{fl}", (x, wy, z+d/2+0.01), (0.08, 0.1, 0.005), m['window'])
        win_b = add_cube(f"{name}_WB{fl}", (x, wy, z-d/2-0.01), (0.08, 0.1, 0.005), m['window'])
        parts.extend([win_f, win_b])

    return parts

# ============================================================
# GROUND (sin plano base grande)
# ============================================================
def build_ground(m):
    parts = []

    # Calles como cubos delgados (no planos)
    road_ns = add_cube("Road_NS", (0, 0.005, 0), (0.8, 0.005, 4.5), m['road'])
    road_ew = add_cube("Road_EW", (0, 0.005, 0), (4.5, 0.005, 0.8), m['road'])
    parts.extend([road_ns, road_ew])

    # Líneas centrales
    for z in [-3.0, -2.0, -1.2, 1.2, 2.0, 3.0]:
        line = add_cube(f"Line_NS_{z}", (0, 0.015, z), (0.03, 0.003, 0.25), m['road_line'])
        parts.append(line)
    for x in [-3.0, -2.0, -1.2, 1.2, 2.0, 3.0]:
        line = add_cube(f"Line_EW_{x}", (x, 0.015, 0), (0.25, 0.003, 0.03), m['road_line'])
        parts.append(line)

    # Sendas peatonales
    dirs_data = [
        ("N", 0, 1.1, False),
        ("S", 0, -1.1, False),
        ("E", 1.1, 0, True),
        ("W", -1.1, 0, True),
    ]
    for d_name, dx, dz, horiz in dirs_data:
        for j in range(5):
            off = (j - 2) * 0.3
            if not horiz:
                s = add_cube(f"CW_{d_name}_{j}", (off, 0.012, dz), (0.1, 0.003, 0.15), m['crosswalk'])
            else:
                s = add_cube(f"CW_{d_name}_{j}", (dx, 0.012, off), (0.15, 0.003, 0.1), m['crosswalk'])
            parts.append(s)

    return parts

# ============================================================
# MAIN
# ============================================================
def main():
    print("=" * 50)
    print("SafeDriver VR - Fix Models v2")
    print("=" * 50)

    clean_scene()
    m = create_mats()

    # Ground
    print("Ground...")
    build_ground(m)

    # Buildings
    print("Buildings...")
    buildings = [
        ("Bld_NW1", (-2.2, 0, 2.2), 1.0, 1.8, 1.0, m['building_blue']),
        ("Bld_NW2", (-3.3, 0, 2.5), 0.7, 1.2, 0.7, m['building_yellow']),
        ("Bld_NE1", (2.2, 0, 2.2), 0.9, 2.2, 0.9, m['building_pink']),
        ("Bld_NE2", (3.2, 0, 3.0), 0.7, 1.0, 0.7, m['building_green']),
        ("Bld_SW1", (-2.2, 0, -2.2), 1.0, 1.5, 1.0, m['building_orange']),
        ("Bld_SW2", (-3.2, 0, -2.8), 0.6, 2.0, 0.6, m['building_purple']),
        ("Bld_SE1", (2.2, 0, -2.2), 1.1, 1.6, 0.9, m['building_green']),
        ("Bld_SE2", (3.0, 0, -3.0), 0.7, 2.5, 0.7, m['building_blue']),
    ]
    for name, pos, w, h, d, bmat in buildings:
        build_building(name, pos, w, h, d, bmat, m)

    # Traffic Lights (facing_angle = dirección que miran las luces)
    print("Traffic lights...")
    build_traffic_light("TrafficLight_North", (0.9, 0, 0.9), 180, m)
    build_traffic_light("TrafficLight_South", (-0.9, 0, -0.9), 0, m)
    build_traffic_light("TrafficLight_East", (0.9, 0, -0.9), 90, m)
    build_traffic_light("TrafficLight_West", (-0.9, 0, 0.9), -90, m)

    # Cars (facing_angle = dirección del frente del auto)
    print("Cars...")
    build_car("Car_Blue", (0.3, 0, -3.0), 0, m['car_blue'], m)      # Mirando +X (norte en NS)
    build_car("Car_Red", (3.0, 0, -0.3), -90, m['car_red'], m)       # Mirando -Z (oeste en EW)

    # Pedestrians
    print("Pedestrians...")
    build_pedestrian("Pedestrian_1", (-1.4, 0, 1.1), m['shirt_blue'], m)
    build_pedestrian("Pedestrian_2", (1.4, 0, -1.1), m['shirt_red'], m)
    build_pedestrian("Pedestrian_3", (-1.1, 0, -1.4), m['shirt_blue'], m)
    build_pedestrian("Pedestrian_4", (1.1, 0, 1.4), m['shirt_red'], m)

    # Trees
    print("Trees...")
    tree_positions = [
        ("Tree_1", (-1.8, 0, 1.5), m['canopy']),
        ("Tree_2", (1.8, 0, 1.5), m['canopy_dk']),
        ("Tree_3", (-1.8, 0, -1.5), m['canopy']),
        ("Tree_4", (1.8, 0, -1.5), m['canopy_dk']),
        ("Tree_5", (-3.5, 0, 0), m['canopy']),
        ("Tree_6", (3.5, 0, 0), m['canopy_dk']),
    ]
    for name, pos, cmat in tree_positions:
        build_tree(name, pos, cmat, m)

    # Export
    print("Exporting...")
    bpy.ops.object.select_all(action='SELECT')
    filepath = os.path.join(EXPORT_DIR, "MiniCity_Complete.fbx")
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        apply_unit_scale=True,
        bake_space_transform=True,
        mesh_smooth_type='FACE',
    )
    print(f"Exported: {filepath}")
    print("=" * 50)
    print("DONE!")

main()
