"""
SafeDriver VR - Fix Models v3
Exporta con orientación correcta para Vuforia/Unity.
La tarjeta es el plano XZ, los edificios crecen en +Y.

Ejecutar en Blender > Scripting > Run Script (Alt+P)
"""

import bpy
import math
import os

EXPORT_DIR = r"F:\Git\SafeDriver\Assets\Models"

def clean_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    for block in [bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights]:
        for b in list(block):
            if b.users == 0:
                block.remove(b)

def mat(name, color, emission=False, strength=5.0):
    m = bpy.data.materials.new(name=name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    if emission:
        bsdf.inputs["Emission Color"].default_value = color
        bsdf.inputs["Emission Strength"].default_value = strength
    return m

def cube(name, loc, scale, material):
    bpy.ops.mesh.primitive_cube_add(location=loc, scale=scale)
    o = bpy.context.active_object
    o.name = name
    bpy.ops.object.transform_apply(scale=True)
    o.data.materials.clear()
    o.data.materials.append(material)
    return o

def cyl(name, loc, r, h, material):
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=r, depth=h, location=loc)
    o = bpy.context.active_object
    o.name = name
    bpy.ops.object.transform_apply()
    o.data.materials.clear()
    o.data.materials.append(material)
    return o

def sphere(name, loc, r, material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=4, radius=r, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.data.materials.clear()
    o.data.materials.append(material)
    return o

def create_mats():
    m = {}
    m['road'] = mat("Road", (0.15, 0.15, 0.17, 1))
    m['sidewalk'] = mat("Sidewalk", (0.55, 0.53, 0.50, 1))
    m['crosswalk'] = mat("Crosswalk", (0.95, 0.95, 0.95, 1))
    m['road_line'] = mat("RoadLine", (0.9, 0.85, 0.2, 1))
    m['bld_blue'] = mat("Bld_Blue", (0.35, 0.55, 0.78, 1))
    m['bld_pink'] = mat("Bld_Pink", (0.82, 0.45, 0.55, 1))
    m['bld_yellow'] = mat("Bld_Yellow", (0.9, 0.8, 0.4, 1))
    m['bld_green'] = mat("Bld_Green", (0.45, 0.72, 0.55, 1))
    m['bld_orange'] = mat("Bld_Orange", (0.9, 0.55, 0.3, 1))
    m['bld_purple'] = mat("Bld_Purple", (0.6, 0.4, 0.75, 1))
    m['window'] = mat("Window", (0.65, 0.82, 0.95, 1))
    m['roof'] = mat("Roof", (0.45, 0.28, 0.22, 1))
    m['pole'] = mat("Pole", (0.3, 0.3, 0.32, 1))
    m['housing'] = mat("TL_Housing", (0.15, 0.15, 0.17, 1))
    m['red_on'] = mat("Light_Red_On", (1, 0.1, 0.1, 1), True, 8)
    m['red_off'] = mat("Light_Red_Off", (0.3, 0.08, 0.08, 1))
    m['yel_on'] = mat("Light_Yel_On", (1, 0.85, 0.1, 1), True, 8)
    m['yel_off'] = mat("Light_Yel_Off", (0.3, 0.25, 0.05, 1))
    m['grn_on'] = mat("Light_Grn_On", (0.1, 1, 0.2, 1), True, 8)
    m['grn_off'] = mat("Light_Grn_Off", (0.05, 0.25, 0.08, 1))
    m['car_blue'] = mat("Car_Blue", (0.2, 0.4, 0.8, 1))
    m['car_red'] = mat("Car_Red", (0.8, 0.15, 0.15, 1))
    m['car_win'] = mat("CarWin", (0.5, 0.65, 0.8, 1))
    m['tire'] = mat("Tire", (0.1, 0.1, 0.1, 1))
    m['headlight'] = mat("Headlight", (1, 0.95, 0.7, 1), True, 2)
    m['skin'] = mat("Skin", (0.85, 0.7, 0.55, 1))
    m['shirt_b'] = mat("ShirtB", (0.3, 0.45, 0.75, 1))
    m['shirt_r'] = mat("ShirtR", (0.75, 0.2, 0.2, 1))
    m['pants'] = mat("Pants", (0.2, 0.2, 0.35, 1))
    m['shoes'] = mat("Shoes", (0.15, 0.12, 0.1, 1))
    m['trunk'] = mat("Trunk", (0.4, 0.25, 0.15, 1))
    m['canopy'] = mat("Canopy", (0.25, 0.6, 0.2, 1))
    m['canopy2'] = mat("Canopy2", (0.15, 0.5, 0.15, 1))
    return m


def build_ground(m):
    """Calles en el plano XZ, Y=0."""
    # Calles (cubos muy finos)
    cube("Road_NS", (0, 0.005, 0), (0.7, 0.005, 4.0), m['road'])
    cube("Road_EW", (0, 0.005, 0), (4.0, 0.005, 0.7), m['road'])

    # Líneas amarillas centrales
    for z in [-2.5, -1.8, -1.2, 1.2, 1.8, 2.5]:
        cube(f"LineNS_{z:.0f}", (0, 0.012, z), (0.02, 0.002, 0.2), m['road_line'])
    for x in [-2.5, -1.8, -1.2, 1.2, 1.8, 2.5]:
        cube(f"LineEW_{x:.0f}", (x, 0.012, 0), (0.2, 0.002, 0.02), m['road_line'])

    # Sendas peatonales
    for j in range(5):
        off = (j - 2) * 0.25
        cube(f"CW_N_{j}", (off, 0.012, 0.95), (0.08, 0.002, 0.12), m['crosswalk'])
        cube(f"CW_S_{j}", (off, 0.012, -0.95), (0.08, 0.002, 0.12), m['crosswalk'])
        cube(f"CW_E_{j}", (0.95, 0.012, off), (0.12, 0.002, 0.08), m['crosswalk'])
        cube(f"CW_W_{j}", (-0.95, 0.012, off), (0.12, 0.002, 0.08), m['crosswalk'])


def build_building(name, x, z, w, h, d, body_mat, m):
    """Edificio: base en XZ, crece en +Y."""
    cube(f"{name}_Body", (x, h/2, z), (w/2, h/2, d/2), body_mat)
    cube(f"{name}_Roof", (x, h+0.04, z), (w/2+0.04, 0.04, d/2+0.04), m['roof'])
    # Ventanas
    for fl in range(int(h / 0.55)):
        wy = 0.35 + fl * 0.55
        cube(f"{name}_WF{fl}", (x, wy, z + d/2 + 0.01), (0.07, 0.08, 0.005), m['window'])
        cube(f"{name}_WB{fl}", (x, wy, z - d/2 - 0.01), (0.07, 0.08, 0.005), m['window'])


def build_traffic_light(name, x, z, face_dir, m):
    """
    Semáforo en (x, 0, z).
    face_dir: 'N','S','E','W' = dirección que miran las luces.
    Las luces crecen en +Y. El frente (luces) mira según face_dir.
    """
    # Poste: cilindro vertical
    cyl(f"{name}_Pole", (x, 0.5, z), 0.035, 1.0, m['pole'])

    # Offset del frente según dirección
    dx, dz = 0, 0
    if face_dir == 'N': dz = 0.055
    elif face_dir == 'S': dz = -0.055
    elif face_dir == 'E': dx = 0.055
    elif face_dir == 'W': dx = -0.055

    # Carcasa
    cube(f"{name}_Housing", (x + dx*0.5, 1.1, z + dz*0.5), (0.07, 0.16, 0.045), m['housing'])

    # Luces (esferitas que sobresalen hacia face_dir)
    sphere(f"{name}_Red",    (x + dx, 1.2, z + dz), 0.035, m['red_on'])
    sphere(f"{name}_Yellow", (x + dx, 1.1, z + dz), 0.035, m['yel_off'])
    sphere(f"{name}_Green",  (x + dx, 1.0, z + dz), 0.035, m['grn_off'])


def build_car(name, x, z, face_dir, body_mat, m):
    """
    Auto en (x, 0, z).
    face_dir: 'N'=+Z, 'S'=-Z, 'E'=+X, 'W'=-X
    """
    # Dimensiones del auto según dirección
    if face_dir in ('N', 'S'):
        # Largo en Z, ancho en X
        bl, bw = 0.3, 0.15  # body length (Z), width (X)
        cube(f"{name}_Body", (x, 0.13, z), (bw, 0.08, bl), body_mat)
        cube(f"{name}_Cabin", (x, 0.27, z), (0.12, 0.07, 0.16), body_mat)

        s = 1 if face_dir == 'N' else -1
        cube(f"{name}_Windshield", (x, 0.27, z + s*0.14), (0.10, 0.05, 0.015), m['car_win'])
        cube(f"{name}_RearWin", (x, 0.27, z - s*0.16), (0.10, 0.05, 0.015), m['car_win'])

        # Faros
        cube(f"{name}_HL_L", (x + 0.07, 0.14, z + s*0.31), (0.02, 0.02, 0.01), m['headlight'])
        cube(f"{name}_HL_R", (x - 0.07, 0.14, z + s*0.31), (0.02, 0.02, 0.01), m['headlight'])

        # Ruedas (cilindros en X)
        for wx in [0.16, -0.16]:
            for wz in [0.18, -0.18]:
                w = cyl(f"{name}_W_{wx}_{wz}", (x + wx, 0.05, z + wz*s), 0.05, 0.03, m['tire'])
                w.rotation_euler.y = math.radians(90)
                bpy.context.view_layer.objects.active = w
                bpy.ops.object.transform_apply(rotation=True)

    else:  # E or W
        bl, bw = 0.3, 0.15
        cube(f"{name}_Body", (x, 0.13, z), (bl, 0.08, bw), body_mat)
        cube(f"{name}_Cabin", (x, 0.27, z), (0.16, 0.07, 0.12), body_mat)

        s = 1 if face_dir == 'E' else -1
        cube(f"{name}_Windshield", (x + s*0.14, 0.27, z), (0.015, 0.05, 0.10), m['car_win'])
        cube(f"{name}_RearWin", (x - s*0.16, 0.27, z), (0.015, 0.05, 0.10), m['car_win'])

        cube(f"{name}_HL_L", (x + s*0.31, 0.14, z + 0.07), (0.01, 0.02, 0.02), m['headlight'])
        cube(f"{name}_HL_R", (x + s*0.31, 0.14, z - 0.07), (0.01, 0.02, 0.02), m['headlight'])

        for wz in [0.16, -0.16]:
            for wx in [0.18, -0.18]:
                w = cyl(f"{name}_W_{wx}_{wz}", (x + wx*s, 0.05, z + wz), 0.05, 0.03, m['tire'])
                w.rotation_euler.x = math.radians(90)
                bpy.context.view_layer.objects.active = w
                bpy.ops.object.transform_apply(rotation=True)


def build_pedestrian(name, x, z, shirt_mat, m):
    sphere(f"{name}_Head", (x, 0.62, z), 0.05, m['skin'])
    cube(f"{name}_Torso", (x, 0.44, z), (0.06, 0.10, 0.04), shirt_mat)
    cube(f"{name}_LegL", (x, 0.2, z + 0.02), (0.03, 0.12, 0.03), m['pants'])
    cube(f"{name}_LegR", (x, 0.2, z - 0.02), (0.03, 0.12, 0.03), m['pants'])
    cube(f"{name}_ArmL", (x, 0.44, z + 0.08), (0.02, 0.08, 0.02), m['skin'])
    cube(f"{name}_ArmR", (x, 0.44, z - 0.08), (0.02, 0.08, 0.02), m['skin'])


def build_tree(name, x, z, canopy_mat, m):
    cyl(f"{name}_Trunk", (x, 0.25, z), 0.035, 0.5, m['trunk'])
    s = sphere(f"{name}_Canopy", (x, 0.65, z), 0.25, canopy_mat)
    s.scale = (1, 0.75, 1)
    bpy.context.view_layer.objects.active = s
    bpy.ops.object.transform_apply(scale=True)


# ============================================================
def main():
    print("=" * 50)
    print("SafeDriver VR - Fix Models v3")
    print("=" * 50)

    clean_scene()
    m = create_mats()

    print("Ground...")
    build_ground(m)

    print("Buildings...")
    build_building("BldNW1", -2.0, 2.0, 0.9, 1.6, 0.9, m['bld_blue'], m)
    build_building("BldNW2", -3.0, 2.3, 0.6, 1.1, 0.6, m['bld_yellow'], m)
    build_building("BldNE1", 2.0, 2.0, 0.8, 2.0, 0.8, m['bld_pink'], m)
    build_building("BldNE2", 2.8, 2.8, 0.6, 0.9, 0.6, m['bld_green'], m)
    build_building("BldSW1", -2.0, -2.0, 0.9, 1.4, 0.9, m['bld_orange'], m)
    build_building("BldSW2", -2.9, -2.6, 0.5, 1.8, 0.5, m['bld_purple'], m)
    build_building("BldSE1", 2.0, -2.0, 1.0, 1.5, 0.8, m['bld_green'], m)
    build_building("BldSE2", 2.7, -2.7, 0.6, 2.2, 0.6, m['bld_blue'], m)

    print("Traffic lights...")
    build_traffic_light("TrafficLight_North", 0.8, 0.8, 'S', m)
    build_traffic_light("TrafficLight_South", -0.8, -0.8, 'N', m)
    build_traffic_light("TrafficLight_East", 0.8, -0.8, 'W', m)
    build_traffic_light("TrafficLight_West", -0.8, 0.8, 'E', m)

    print("Cars...")
    build_car("Car_Blue", 0.25, -2.5, 'N', m['car_blue'], m)
    build_car("Car_Red", 2.5, 0.25, 'W', m['car_red'], m)

    print("Pedestrians...")
    build_pedestrian("Pedestrian_1", -1.2, 0.95, m['shirt_b'], m)
    build_pedestrian("Pedestrian_2", 1.2, -0.95, m['shirt_r'], m)
    build_pedestrian("Pedestrian_3", -0.95, -1.2, m['shirt_b'], m)
    build_pedestrian("Pedestrian_4", 0.95, 1.2, m['shirt_r'], m)

    print("Trees...")
    build_tree("Tree_1", -1.6, 1.4, m['canopy'], m)
    build_tree("Tree_2", 1.6, 1.4, m['canopy2'], m)
    build_tree("Tree_3", -1.6, -1.4, m['canopy'], m)
    build_tree("Tree_4", 1.6, -1.4, m['canopy2'], m)
    build_tree("Tree_5", -3.2, 0, m['canopy'], m)
    build_tree("Tree_6", 3.2, 0, m['canopy2'], m)

    # EXPORT - Unity FBX settings
    print("Exporting...")
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(EXPORT_DIR, "MiniCity_Complete.fbx"),
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='Z',
        axis_up='Y',
        apply_unit_scale=True,
        bake_space_transform=True,
        mesh_smooth_type='FACE',
    )

    print("=" * 50)
    print("DONE! Exported MiniCity_Complete.fbx")
    print("=" * 50)

main()
