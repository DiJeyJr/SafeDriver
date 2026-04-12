"""
SafeDriver VR - Mini City AR Card
Script de Blender para crear todos los modelos de la mini ciudad.
Ejecutar en Blender > Scripting > Run Script (Alt+P)

Exporta los modelos como FBX individuales en la misma carpeta del proyecto.
"""

import bpy
import bmesh
import os
import math

# ============================================================
# CONFIG
# ============================================================
EXPORT_DIR = r"F:\Git\SafeDriver\Assets\Models"
SCALE = 1.0  # Metros reales, se escala en Unity

# ============================================================
# UTILS
# ============================================================
def clean_scene():
    """Elimina todos los objetos de la escena."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    for c in bpy.data.collections:
        if c.name != "Collection":
            bpy.data.collections.remove(c)

def new_material(name, color, emission=False, emission_strength=5.0):
    """Crea un material con color base. Opcionalmente emisivo."""
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    if emission:
        bsdf.inputs["Emission Color"].default_value = color
        bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat

def assign_material(obj, mat):
    """Asigna material a un objeto."""
    obj.data.materials.clear()
    obj.data.materials.append(mat)

def create_cube(name, location, scale, material=None):
    """Crea un cubo con nombre, posición, escala y material."""
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    if material:
        assign_material(obj, material)
    return obj

def create_cylinder(name, location, radius, depth, material=None):
    """Crea un cilindro."""
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=8, radius=radius, depth=depth, location=location
    )
    obj = bpy.context.active_object
    obj.name = name
    if material:
        assign_material(obj, material)
    return obj

def create_sphere(name, location, radius, material=None, segments=8):
    """Crea una esfera low-poly."""
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments, ring_count=segments // 2,
        radius=radius, location=location
    )
    obj = bpy.context.active_object
    obj.name = name
    bpy.ops.object.shade_smooth()
    if material:
        assign_material(obj, material)
    return obj

def create_plane(name, location, size, material=None):
    """Crea un plano."""
    bpy.ops.mesh.primitive_plane_add(size=size, location=location)
    obj = bpy.context.active_object
    obj.name = name
    if material:
        assign_material(obj, material)
    return obj

def parent_objects(parent, children):
    """Emparenta una lista de objetos a un padre."""
    for child in children:
        child.parent = parent

def export_fbx(objects, filename, folder):
    """Exporta una lista de objetos como FBX."""
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
        for child in obj.children_recursive:
            child.select_set(True)
    filepath = os.path.join(folder, filename)
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

def get_collection(name):
    """Obtiene o crea una colección."""
    if name in bpy.data.collections:
        return bpy.data.collections[name]
    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    return col

def move_to_collection(obj, collection):
    """Mueve objeto a colección."""
    for c in obj.users_collection:
        c.objects.unlink(obj)
    collection.objects.link(obj)

# ============================================================
# MATERIALS
# ============================================================
def create_materials():
    mats = {}
    # Calzada
    mats['road'] = new_material("Road", (0.15, 0.15, 0.17, 1.0))
    mats['sidewalk'] = new_material("Sidewalk", (0.6, 0.58, 0.55, 1.0))
    mats['crosswalk'] = new_material("Crosswalk", (0.95, 0.95, 0.95, 1.0))
    mats['road_line'] = new_material("RoadLine", (0.9, 0.9, 0.2, 1.0))

    # Edificios
    mats['building_blue'] = new_material("Building_Blue", (0.35, 0.55, 0.78, 1.0))
    mats['building_pink'] = new_material("Building_Pink", (0.82, 0.45, 0.55, 1.0))
    mats['building_yellow'] = new_material("Building_Yellow", (0.9, 0.8, 0.4, 1.0))
    mats['building_green'] = new_material("Building_Green", (0.45, 0.72, 0.55, 1.0))
    mats['building_orange'] = new_material("Building_Orange", (0.9, 0.55, 0.3, 1.0))
    mats['building_purple'] = new_material("Building_Purple", (0.6, 0.4, 0.75, 1.0))
    mats['window'] = new_material("Window", (0.7, 0.85, 0.95, 1.0))
    mats['roof'] = new_material("Roof", (0.5, 0.3, 0.25, 1.0))

    # Semáforo
    mats['pole'] = new_material("Pole", (0.25, 0.25, 0.27, 1.0))
    mats['traffic_housing'] = new_material("TrafficHousing", (0.12, 0.12, 0.14, 1.0))
    mats['light_red_on'] = new_material("Light_Red_On", (1.0, 0.1, 0.1, 1.0), emission=True, emission_strength=8.0)
    mats['light_red_off'] = new_material("Light_Red_Off", (0.3, 0.08, 0.08, 1.0))
    mats['light_yellow_on'] = new_material("Light_Yellow_On", (1.0, 0.85, 0.1, 1.0), emission=True, emission_strength=8.0)
    mats['light_yellow_off'] = new_material("Light_Yellow_Off", (0.3, 0.25, 0.05, 1.0))
    mats['light_green_on'] = new_material("Light_Green_On", (0.1, 1.0, 0.2, 1.0), emission=True, emission_strength=8.0)
    mats['light_green_off'] = new_material("Light_Green_Off", (0.05, 0.25, 0.08, 1.0))

    # Vehículo
    mats['car_body_blue'] = new_material("CarBody_Blue", (0.2, 0.4, 0.8, 1.0))
    mats['car_body_red'] = new_material("CarBody_Red", (0.8, 0.15, 0.15, 1.0))
    mats['car_window'] = new_material("CarWindow", (0.5, 0.65, 0.8, 1.0))
    mats['tire'] = new_material("Tire", (0.1, 0.1, 0.1, 1.0))
    mats['headlight'] = new_material("Headlight", (1.0, 0.95, 0.7, 1.0), emission=True, emission_strength=2.0)

    # Peatón
    mats['skin'] = new_material("Skin", (0.85, 0.7, 0.55, 1.0))
    mats['shirt_blue'] = new_material("Shirt_Blue", (0.3, 0.45, 0.75, 1.0))
    mats['shirt_red'] = new_material("Shirt_Red", (0.75, 0.2, 0.2, 1.0))
    mats['pants'] = new_material("Pants", (0.2, 0.2, 0.35, 1.0))
    mats['shoes'] = new_material("Shoes", (0.15, 0.12, 0.1, 1.0))

    # Árboles
    mats['trunk'] = new_material("TreeTrunk", (0.4, 0.25, 0.15, 1.0))
    mats['canopy'] = new_material("TreeCanopy", (0.25, 0.6, 0.2, 1.0))
    mats['canopy_dark'] = new_material("TreeCanopy_Dark", (0.15, 0.45, 0.15, 1.0))

    return mats

# ============================================================
# MODEL BUILDERS
# ============================================================

def build_ground(mats):
    """Crea el suelo: intersección con calles y veredas."""
    parts = []

    # Base de vereda (plano grande)
    base = create_plane("Ground_Base", (0, 0, 0), 10.0, mats['sidewalk'])
    parts.append(base)

    # Calle Norte-Sur
    road_ns = create_cube("Road_NS", (0, 0.01, 0), (1.0, 0.01, 5.0), mats['road'])
    parts.append(road_ns)

    # Calle Este-Oeste
    road_ew = create_cube("Road_EW", (0, 0.01, 0), (5.0, 0.01, 1.0), mats['road'])
    parts.append(road_ew)

    # Líneas centrales (amarillas)
    for z in [-3.5, -2.5, -1.5, 1.5, 2.5, 3.5]:
        line = create_cube(f"Line_NS_{z}", (0, 0.025, z), (0.03, 0.005, 0.3), mats['road_line'])
        parts.append(line)
    for x in [-3.5, -2.5, -1.5, 1.5, 2.5, 3.5]:
        line = create_cube(f"Line_EW_{x}", (x, 0.025, 0), (0.3, 0.005, 0.03), mats['road_line'])
        parts.append(line)

    # Sendas peatonales (4 cruces)
    crosswalk_positions = [
        (0, 0.02, 1.3, 0),     # Norte
        (0, 0.02, -1.3, 0),    # Sur
        (1.3, 0.02, 0, 90),    # Este
        (-1.3, 0.02, 0, 90),   # Oeste
    ]
    for i, (cx, cy, cz, rot) in enumerate(crosswalk_positions):
        dirs = ["N", "S", "E", "W"]
        for j in range(5):
            offset = (j - 2) * 0.35
            if rot == 0:
                stripe = create_cube(
                    f"Crosswalk_{dirs[i]}_{j}",
                    (offset, cy, cz),
                    (0.12, 0.005, 0.2),
                    mats['crosswalk']
                )
            else:
                stripe = create_cube(
                    f"Crosswalk_{dirs[i]}_{j}",
                    (cx, cy, offset),
                    (0.2, 0.005, 0.12),
                    mats['crosswalk']
                )
            parts.append(stripe)

    return parts

def build_building(name, location, width, height, depth, mat_body, mat_window, mat_roof):
    """Crea un edificio con ventanas y techo."""
    parts = []
    x, y, z = location

    # Cuerpo principal
    body = create_cube(
        f"{name}_Body",
        (x, y + height / 2, z),
        (width / 2, height / 2, depth / 2),
        mat_body
    )
    parts.append(body)

    # Techo
    roof = create_cube(
        f"{name}_Roof",
        (x, y + height + 0.05, z),
        (width / 2 + 0.05, 0.05, depth / 2 + 0.05),
        mat_roof
    )
    parts.append(roof)

    # Ventanas (frente y costados)
    floors = int(height / 0.6)
    windows_per_floor = max(1, int(width / 0.5))

    for floor in range(floors):
        wy = y + 0.4 + floor * 0.6
        for w in range(windows_per_floor):
            wx = x - (width / 2) * 0.6 + w * (width * 0.6 / max(1, windows_per_floor - 1)) if windows_per_floor > 1 else x
            # Ventana frontal
            win = create_cube(
                f"{name}_Win_F{floor}_{w}",
                (wx, wy, z + depth / 2 + 0.01),
                (0.08, 0.1, 0.005),
                mat_window
            )
            parts.append(win)
            # Ventana trasera
            win_b = create_cube(
                f"{name}_Win_B{floor}_{w}",
                (wx, wy, z - depth / 2 - 0.01),
                (0.08, 0.1, 0.005),
                mat_window
            )
            parts.append(win_b)

    return parts

def build_all_buildings(mats):
    """Crea todos los edificios de la mini ciudad."""
    buildings = []
    configs = [
        # name, (x,y,z), width, height, depth, material
        ("Building_NW_1", (-2.5, 0, 2.5), 1.2, 1.8, 1.2, mats['building_blue']),
        ("Building_NW_2", (-3.8, 0, 2.8), 0.8, 1.2, 0.8, mats['building_yellow']),
        ("Building_NE_1", (2.5, 0, 2.5), 1.0, 2.2, 1.0, mats['building_pink']),
        ("Building_NE_2", (3.5, 0, 3.2), 0.9, 1.0, 0.9, mats['building_green']),
        ("Building_SW_1", (-2.5, 0, -2.5), 1.1, 1.5, 1.1, mats['building_orange']),
        ("Building_SW_2", (-3.5, 0, -3.0), 0.7, 2.0, 0.7, mats['building_purple']),
        ("Building_SE_1", (2.5, 0, -2.5), 1.3, 1.6, 1.0, mats['building_green']),
        ("Building_SE_2", (3.3, 0, -3.3), 0.8, 2.5, 0.8, mats['building_blue']),
    ]
    for name, loc, w, h, d, mat in configs:
        parts = build_building(name, loc, w, h, d, mat, mats['window'], mats['roof'])
        buildings.extend(parts)
    return buildings

def build_traffic_light(name, location, rotation_z, mats):
    """Crea un semáforo completo."""
    parts = []
    x, y, z = location

    # Poste
    pole = create_cylinder(f"{name}_Pole", (x, y + 0.6, z), 0.04, 1.2, mats['pole'])
    parts.append(pole)

    # Carcasa
    housing = create_cube(f"{name}_Housing", (x, y + 1.3, z), (0.08, 0.18, 0.06), mats['traffic_housing'])
    parts.append(housing)

    # Luces (de arriba a abajo: rojo, amarillo, verde)
    light_r = create_sphere(f"{name}_Red", (x, y + 1.42, z + 0.065), 0.04, mats['light_red_on'], segments=8)
    light_y = create_sphere(f"{name}_Yellow", (x, y + 1.3, z + 0.065), 0.04, mats['light_yellow_off'], segments=8)
    light_g = create_sphere(f"{name}_Green", (x, y + 1.18, z + 0.065), 0.04, mats['light_green_off'], segments=8)
    parts.extend([light_r, light_y, light_g])

    # Rotar todo el semáforo
    if rotation_z != 0:
        for p in parts:
            p.rotation_euler.z = math.radians(rotation_z)
            # Ajustar posición rotada
            rad = math.radians(rotation_z)
            ox, oz = p.location.x - x, p.location.z - z
            p.location.x = x + ox * math.cos(rad) - oz * math.sin(rad)
            p.location.z = z + ox * math.sin(rad) + oz * math.cos(rad)

    return parts

def build_all_traffic_lights(mats):
    """Crea 4 semáforos en la intersección."""
    all_parts = []

    configs = [
        ("TrafficLight_North", (1.15, 0, 1.15), 180),
        ("TrafficLight_South", (-1.15, 0, -1.15), 0),
        ("TrafficLight_East", (1.15, 0, -1.15), 90),
        ("TrafficLight_West", (-1.15, 0, 1.15), -90),
    ]
    for name, loc, rot in configs:
        parts = build_traffic_light(name, loc, rot, mats)
        all_parts.extend(parts)

    return all_parts

def build_car(name, location, mat_body, mats):
    """Crea un auto sedan low-poly."""
    parts = []
    x, y, z = location

    # Carrocería inferior
    body_low = create_cube(f"{name}_BodyLow", (x, y + 0.15, z), (0.3, 0.1, 0.15), mat_body)
    parts.append(body_low)

    # Carrocería superior (cabina)
    body_top = create_cube(f"{name}_BodyTop", (x - 0.02, y + 0.32, z), (0.18, 0.08, 0.13), mat_body)
    parts.append(body_top)

    # Parabrisas
    windshield = create_cube(f"{name}_Windshield", (x + 0.15, y + 0.32, z), (0.02, 0.06, 0.11), mats['car_window'])
    parts.append(windshield)

    # Luneta trasera
    rear_window = create_cube(f"{name}_RearWindow", (x - 0.18, y + 0.32, z), (0.02, 0.06, 0.11), mats['car_window'])
    parts.append(rear_window)

    # Ruedas
    wheel_positions = [
        (x + 0.18, y + 0.06, z + 0.15),
        (x + 0.18, y + 0.06, z - 0.15),
        (x - 0.18, y + 0.06, z + 0.15),
        (x - 0.18, y + 0.06, z - 0.15),
    ]
    for i, wp in enumerate(wheel_positions):
        wheel = create_cylinder(f"{name}_Wheel_{i}", wp, 0.06, 0.04, mats['tire'])
        wheel.rotation_euler.x = math.radians(90)
        parts.append(wheel)

    # Faros
    hl_l = create_cube(f"{name}_HeadlightL", (x + 0.31, y + 0.17, z + 0.08), (0.01, 0.025, 0.025), mats['headlight'])
    hl_r = create_cube(f"{name}_HeadlightR", (x + 0.31, y + 0.17, z - 0.08), (0.01, 0.025, 0.025), mats['headlight'])
    parts.extend([hl_l, hl_r])

    return parts

def build_pedestrian(name, location, mat_shirt, mats):
    """Crea un peatón estilizado (simple)."""
    parts = []
    x, y, z = location

    # Cabeza
    head = create_sphere(f"{name}_Head", (x, y + 0.7, z), 0.06, mats['skin'], segments=8)
    parts.append(head)

    # Torso
    torso = create_cube(f"{name}_Torso", (x, y + 0.5, z), (0.07, 0.12, 0.05), mat_shirt)
    parts.append(torso)

    # Piernas
    leg_l = create_cube(f"{name}_LegL", (x, y + 0.22, z + 0.025), (0.035, 0.15, 0.035), mats['pants'])
    leg_r = create_cube(f"{name}_LegR", (x, y + 0.22, z - 0.025), (0.035, 0.15, 0.035), mats['pants'])
    parts.extend([leg_l, leg_r])

    # Brazos
    arm_l = create_cube(f"{name}_ArmL", (x, y + 0.5, z + 0.1), (0.025, 0.1, 0.025), mats['skin'])
    arm_r = create_cube(f"{name}_ArmR", (x, y + 0.5, z - 0.1), (0.025, 0.1, 0.025), mats['skin'])
    parts.extend([arm_l, arm_r])

    # Zapatos
    shoe_l = create_cube(f"{name}_ShoeL", (x + 0.01, y + 0.04, z + 0.025), (0.035, 0.02, 0.03), mats['shoes'])
    shoe_r = create_cube(f"{name}_ShoeR", (x + 0.01, y + 0.04, z - 0.025), (0.035, 0.02, 0.03), mats['shoes'])
    parts.extend([shoe_l, shoe_r])

    return parts

def build_tree(name, location, mats, canopy_mat=None):
    """Crea un árbol low-poly."""
    parts = []
    x, y, z = location
    mat_c = canopy_mat or mats['canopy']

    # Tronco
    trunk = create_cylinder(f"{name}_Trunk", (x, y + 0.3, z), 0.04, 0.6, mats['trunk'])
    parts.append(trunk)

    # Copa (esfera aplastada)
    canopy = create_sphere(f"{name}_Canopy", (x, y + 0.8, z), 0.3, mat_c, segments=6)
    canopy.scale.y = 0.7
    bpy.context.view_layer.objects.active = canopy
    bpy.ops.object.transform_apply(scale=True)
    parts.append(canopy)

    return parts

# ============================================================
# MAIN
# ============================================================
def main():
    print("=" * 50)
    print("SafeDriver VR - Mini City Builder")
    print("=" * 50)

    clean_scene()
    mats = create_materials()

    all_objects = []

    # --- GROUND ---
    print("Building ground...")
    ground_parts = build_ground(mats)
    all_objects.extend(ground_parts)

    # --- BUILDINGS ---
    print("Building buildings...")
    building_parts = build_all_buildings(mats)
    all_objects.extend(building_parts)

    # --- TRAFFIC LIGHTS ---
    print("Building traffic lights...")
    traffic_parts = build_all_traffic_lights(mats)
    all_objects.extend(traffic_parts)

    # --- CARS ---
    print("Building cars...")
    car1_parts = build_car("Car_Blue", (0, 0, -3.0), mats['car_body_blue'], mats)
    car2_parts = build_car("Car_Red", (3.0, 0, 0), mats['car_body_red'], mats)
    # Rotar car2 para que vaya Este-Oeste
    for p in car2_parts:
        ox, oz = p.location.x - 3.0, p.location.z
        p.location.x = 3.0 + oz
        p.location.z = -ox
        p.rotation_euler.y += math.radians(90)
    all_objects.extend(car1_parts)
    all_objects.extend(car2_parts)

    # --- PEDESTRIANS ---
    print("Building pedestrians...")
    ped_configs = [
        ("Pedestrian_1", (-1.6, 0, 1.3), mats['shirt_blue']),
        ("Pedestrian_2", (1.6, 0, -1.3), mats['shirt_red']),
        ("Pedestrian_3", (-1.3, 0, -1.6), mats['shirt_blue']),
        ("Pedestrian_4", (1.3, 0, 1.6), mats['shirt_red']),
    ]
    for name, loc, shirt_mat in ped_configs:
        ped_parts = build_pedestrian(name, loc, shirt_mat, mats)
        all_objects.extend(ped_parts)

    # --- TREES ---
    print("Building trees...")
    tree_configs = [
        ("Tree_1", (-2.0, 0, 1.6), mats['canopy']),
        ("Tree_2", (2.0, 0, 1.6), mats['canopy_dark']),
        ("Tree_3", (-2.0, 0, -1.6), mats['canopy']),
        ("Tree_4", (2.0, 0, -1.6), mats['canopy_dark']),
        ("Tree_5", (-4.0, 0, 0), mats['canopy']),
        ("Tree_6", (4.0, 0, 0), mats['canopy_dark']),
    ]
    for name, loc, canopy_mat in tree_configs:
        tree_parts = build_tree(name, loc, mats, canopy_mat)
        all_objects.extend(tree_parts)

    # --- EXPORT ALL AS SINGLE FBX ---
    print("Exporting complete city...")
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
    print(f"Exported complete city: {filepath}")

    # --- EXPORT INDIVIDUAL PIECES ---
    # Semáforo individual (para prefab)
    print("Exporting individual traffic light...")
    traffic_light_objs = [o for o in all_objects if o.name.startswith("TrafficLight_North")]
    export_fbx(traffic_light_objs, "Props/TrafficLight.fbx", EXPORT_DIR)

    # Auto azul (para prefab)
    print("Exporting individual car...")
    car_blue_objs = [o for o in all_objects if o.name.startswith("Car_Blue")]
    export_fbx(car_blue_objs, "Vehicles/Car_Blue.fbx", EXPORT_DIR)

    car_red_objs = [o for o in all_objects if o.name.startswith("Car_Red")]
    export_fbx(car_red_objs, "Vehicles/Car_Red.fbx", EXPORT_DIR)

    # Peatón individual (para prefab)
    print("Exporting individual pedestrian...")
    ped1_objs = [o for o in all_objects if o.name.startswith("Pedestrian_1")]
    export_fbx(ped1_objs, "Characters/Pedestrian_Blue.fbx", EXPORT_DIR)

    ped2_objs = [o for o in all_objects if o.name.startswith("Pedestrian_2")]
    export_fbx(ped2_objs, "Characters/Pedestrian_Red.fbx", EXPORT_DIR)

    print("=" * 50)
    print("DONE! All models exported to:", EXPORT_DIR)
    print("=" * 50)

main()
