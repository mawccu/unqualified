import bpy, math, os
from mathutils import Vector
OUT=os.path.dirname(os.path.abspath(__file__))
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
def mat(n,c):
 m=bpy.data.materials.new(n); m.diffuse_color=(*c,1); m.use_nodes=True
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'); p.inputs['Base Color'].default_value=(*c,1); p.inputs['Roughness'].default_value=.72
 return m
skin=mat('Warm terracotta',(.55,.255,.135)); dark=mat('Espresso',(.035,.027,.028)); shirt=mat('Ivory shirt',(.78,.74,.61)); vest=mat('Petrol uniform',(.035,.16,.17)); gold=mat('Mustard tie',(.75,.37,.065)); white=mat('Warm eye whites',(.9,.88,.76)); sole=mat('Rubber sole',(.018,.026,.031)); badge=mat('Badge paper',(.82,.79,.65))
parts=[]
def box(n,p,s,m,b=.07):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p); o=bpy.context.object; o.name=n; o.dimensions=s; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 mod=o.modifiers.new('Soft tailored edges','BEVEL'); mod.width=b; mod.segments=3
 bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name)
 o.data.materials.append(m); parts.append(o); return o
def ell(n,p,s,m):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=10,location=p); o=bpy.context.object; o.name=n; o.scale=s; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(m)
 for f in o.data.polygons:f.use_smooth=True
 parts.append(o);return o
def limb(n,a,b,r,m):
 mid=(Vector(a)+Vector(b))/2; o=box(n,mid,(r,r,(Vector(b)-Vector(a)).length+r*.3),m,r*.4); o.rotation_euler=(Vector(b)-Vector(a)).to_track_quat('Z','Y').to_euler(); return o
for side in [-1,1]:
 x=side*.19
 box('Shoe sole', (x,-.12,.07),(.30,.54,.12),sole,.045)
 box('Oversized work shoe',(x,-.14,.17),(.29,.49,.22),dark,.09)
 limb('Trouser leg',(x,.02,.29),(x*.86,.04,.89),.21,vest)
 box('Rolled trouser hem',(x,.01,.34),(.23,.23,.09),dark,.02)
box('Tailored torso',(0,.025,1.14),(.65,.37,.65),vest,.14)
box('Shirt chest',(0,-.177,1.36),(.26,.05,.28),shirt,.035)
for side in [-1,1]:
 o=box('Folded collar',(side*.092,-.22,1.465),(.14,.07,.17),shirt,.018);o.rotation_euler.y=side*.42
 a=(side*.36,.02,1.41);b=(side*.49,-.015,1.12);c=(side*.53,-.12,.91)
 limb('Shirt upper arm',a,b,.20,shirt);limb('Rolled sleeve',b,c,.16,shirt)
 box('Cuff',(side*.53,-.12,.94),(.19,.19,.10),shirt,.025)
 ell('Mitten',(side*.54,-.13,.81),(.11,.095,.15),skin)
 ell('Thumb',(side*.45,-.18,.83),(.055,.065,.09),skin)
ell('Neck',(0,0,1.58),(.115,.12,.19),skin)
head=box('Sculpted rounded oblong head',(0,-.015,1.94),(.52,.43,.65),skin,.19)
for side in [-1,1]:
 ell('Ear',(side*.277,-.005,1.91),(.065,.07,.10),skin)
 ell('Eye white',(side*.117,-.223,1.99),(.088,.037,.093),white)
 ell('Pupil',(side*.117+.012,-.256,1.974),(.027,.014,.038),dark)
 lid=box('Heavy eyelid',(side*.117,-.247,2.047),(.18,.045,.073),skin,.025);lid.rotation_euler.y=side*.08
 brow=box('Tired brow',(side*.115,-.231,2.13),(.18,.044,.036),dark,.012);brow.rotation_euler.y=-side*.15
ell('Long blunt nose',(.013,-.277,1.91),(.075,.105,.115),skin)
for side in [-1,1]:
 o=ell('Mustache',(side*.064,-.245,1.807),(.085,.035,.026),dark);o.rotation_euler.y=side*.17
box('Understated mouth',(.014,-.227,1.754),(.10,.018,.014),dark,.006)
box('Hair back',(0,.12,2.055),(.54,.25,.38),dark,.12)
for i in range(5):
 o=box('Swept hair lock',(-.205+i*.092,-.015,2.254+i*.009),(.14,.40,.145),dark,.065);o.rotation_euler.y=-.18
tie=box('Crooked tie',(0.025,-.22,1.22),(.095,.04,.24),gold,.018);tie.rotation_euler.y=-.16
box('Tie knot',(0,-.228,1.385),(.075,.05,.075),gold,.02)
box('Breast pocket',(-.205,-.173,1.25),(.16,.045,.13),dark,.018)
box('Name badge',(-.205,-.205,1.28),(.135,.015,.063),badge,.008)
for z in [1.1,.99]:ell('Brass button',(.035,-.177,z),(.018,.013,.018),gold)
def text(n,body,p,size,m,rot=(math.pi/2,0,0)):
 cu=bpy.data.curves.new(n,'FONT');cu.body=body;cu.size=size;cu.align_x='CENTER';cu.extrude=.0003;o=bpy.data.objects.new(n,cu);bpy.context.collection.objects.link(o);o.location=p;o.rotation_euler=rot;o.data.materials.append(m);return o
label=text('Badge lettering','STAFF',(-.205,-.217,1.267),.026,dark);parts.append(label)
# Export the actual character only, with applied geometry; this study is not rigged.
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=head
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'worker_01.glb'),use_selection=True)
# Editorial studio presentation.
ground=mat('Backdrop',(.065,.105,.12))
box('Studio ground',(0,0,-.075),(200,200,.1),ground,.01)
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
for name,p,power,size in [('Key',(-3,-4,6),550,4),('Fill',(4,-2,3),300,3),('Rim',(1,3,4),650,3)]:
 bpy.ops.object.light_add(type='AREA',location=p);o=bpy.context.object;o.name=name;o.data.energy=power;o.data.shape='DISK';o.data.size=size;aim(o,(0,0,1.2))
bpy.ops.object.camera_add(location=(3.2,-6.5,3));cam=bpy.context.object;aim(cam,(0,0,1.18));cam.data.type='ORTHO';cam.data.ortho_scale=3.1;bpy.context.scene.camera=cam
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=32;s.cycles.use_denoising=True;s.render.resolution_x=1000;s.render.resolution_y=1000;s.render.resolution_percentage=100
s.world.color=(.18,.18,.18);s.render.image_settings.file_format='PNG';s.render.filepath=os.path.join(OUT,'worker_01.png')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'worker_01.blend'))
bpy.ops.render.render(write_still=True)
print('WORKER_COMPLETE',OUT)
