"""Original UNQUALIFIED wedding prototype assets. Run with Blender --background --python."""
import bpy, math, os, json
from mathutils import Vector
OUT=os.path.dirname(os.path.abspath(__file__))
os.makedirs(os.path.join(OUT,'models'),exist_ok=True)
os.makedirs(os.path.join(OUT,'previews'),exist_ok=True)
os.makedirs(os.path.join(OUT,'source'),exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def material(n,c,metal=0):
 m=bpy.data.materials.new(n);m.diffuse_color=(*c,1);m.use_nodes=True
 p=next(x for x in m.node_tree.nodes if x.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=.64;p.inputs['Metallic'].default_value=metal
 return m
cream=material('Warm linen',(.82,.76,.62));teal=material('Petrol',(.025,.19,.20));wood=material('Walnut',(.17,.075,.035));brass=material('Brushed brass',(.55,.30,.075),.55);black=material('Charcoal',(.023,.031,.036));pink=material('Dusty rose',(.57,.21,.23));leaf=material('Sage',(.16,.29,.20));white=material('Frosting',(.94,.85,.69));silver=material('Steel',(.34,.40,.42),.5);blue=material('Blue cable',(.045,.18,.45));red=material('Red cable',(.55,.055,.035));gold=material('Mustard',(.76,.40,.045));wall=material('Plaster',(.58,.62,.53));floor=material('Oak floor',(.32,.17,.075))
items={};records=[];current=[]
def track(o,n,m):
 o.name=n;o.data.materials.append(m);current.append(o);return o
def cube(n,p,s,m,b=.025):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.dimensions=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 mod=o.modifiers.new('Edge bevel','BEVEL');mod.width=min(b,min(s)*.4);mod.segments=2;bpy.ops.object.modifier_apply(modifier=mod.name);return track(o,n,m)
def cyl(n,p,r,h,m):
 bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=r,depth=h,location=p);return track(bpy.context.object,n,m)
def ball(n,p,s,m):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,location=p);o=bpy.context.object;o.scale=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 for f in o.data.polygons:f.use_smooth=True
 return track(o,n,m)
def bar(n,a,b,r,m):
 o=cyl(n,(Vector(a)+Vector(b))/2,r,(Vector(b)-Vector(a)).length,m);o.rotation_euler=(Vector(b)-Vector(a)).to_track_quat('Z','Y').to_euler();return o
def label(s,p,size=.10,m=black):
 cu=bpy.data.curves.new('Label','FONT');cu.body=s;cu.align_x='CENTER';cu.size=size;cu.extrude=.0005;o=bpy.data.objects.new(s,cu);bpy.context.collection.objects.link(o);o.location=p;o.rotation_euler=(math.pi/2,0,0);return track(o,s,m)
def curve(n,coords,r,m):
 cu=bpy.data.curves.new(n,'CURVE');cu.dimensions='3D';cu.bevel_depth=r;cu.bevel_resolution=2;sp=cu.splines.new('POLY');sp.points.add(len(coords)-1)
 for a,b in zip(sp.points,coords):a.co=(*b,1)
 o=bpy.data.objects.new(n,cu);bpy.context.collection.objects.link(o);return track(o,n,m)
def finish(n,role,collision='box'):
 global current
 bpy.ops.object.select_all(action='DESELECT')
 for o in current:o.select_set(True)
 bpy.context.view_layer.objects.active=current[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join();o=bpy.context.object;o.name=n
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'models',n+'.glb'),use_selection=True)
 triangles=sum(len(p.vertices)-2 for p in o.data.polygons)
 items[n]=o;records.append(dict(id=n,path='models/'+n+'.glb',purpose=role,triangles=triangles,collision_hint=collision,rigged=False,dimensions_blender_xyz=list(o.dimensions)))
 o.hide_render=True;o.hide_set(True);current=[];return o
def table(n,length=1.8,width=.75,height=.85):
 cube('Top',(0,0,height),(length,width,.09),wood)
 for x in [-length*.4,length*.4]:
  for y in [-width*.35,width*.35]:cube('Leg',(x,y,height/2),(.075,.075,height),teal)
 finish(n,'Work surface; anchor sockets separately')
table('prep_table');table('dessert_table',2,.85,.9);table('sound_desk',1.35,.65,.9)
cyl('Linen tabletop',(0,0,.79),.73,.10,cream);cyl('Pedestal',(0,0,.4),.085,.7,brass);cyl('Foot',(0,0,.055),.4,.08,wood);finish('guest_table','Guest seating outcome')
cube('Seat',(0,0,.45),(.43,.44,.08),cream);cube('Back',(0,.19,.76),(.43,.07,.51),teal,.07)
for x in [-.17,.17]:
 for y in [-.17,.17]:cube('Leg',(x,y,.23),(.045,.045,.44),wood)
finish('guest_chair','Venue seating')
cyl('Base',(0,0,.035),.23,.07,brass);cyl('Stem',(0,0,.14),.055,.23,brass);cyl('Tray',(0,0,.27),.39,.045,cream);finish('cake_stand','Cake placement target','convex')
for n,r,h in [('cake_base',.32,.23),('cake_middle',.24,.20),('cake_top',.16,.18)]:
 cyl('Cake',(0,0,h/2),r,h,pink);cyl('Icing',(0,0,h-.012),r+.008,.045,white)
 for i in range(16):
  a=i*math.tau/16;ball('Piped frosting',(math.cos(a)*r*.92,math.sin(a)*r*.92,h+.013),(.027,.027,.021),white)
 finish(n,'Carryable independent cake layer; gameplay sets stacking order','convex')
for x in [-.045,.045]:
 bar('Topper stem',(x,0,0),(x,0,.12),.012,brass);ball('Topper heart',(x,0,.16),(.065,.025,.055),pink)
finish('cake_topper','Final cake placement','convex')
for i,(x,y,r) in enumerate([(-.11,0,.12),(.08,.03,.16),(.21,-.05,.08),(-.16,-.12,.06)]):ball('Frosting patch',(x,y,.012),(r,r*.7,.014),pink)
finish('frosting_spill','Toggleable evidence mesh; no solid collision','none')
cube('Cloth',(0,0,.025),(.28,.20,.04),teal);finish('cleaning_cloth','Cleaning interaction prop')
cyl('Bucket',(0,0,.15),.16,.30,teal);cyl('Water surface',(0,0,.302),.14,.006,blue);curve('Handle',[(-.17,0,.23),(-.17,0,.46),(.17,0,.46),(.17,0,.23)],.012,silver);finish('mop_bucket','Wash-up nook','convex')
bar('Mop handle',(0,0,.08),(0,0,1.3),.018,wood);cube('Mop head',(0,0,.035),(.34,.17,.065),cream);finish('mop','Cleaning tool','convex')
cube('Crate',(0,0,.23),(.58,.42,.46),teal)
for z in [.09,.34]:
 for x in [-.29,.29]:cube('Reinforcing rail',(x,0,z),(.035,.45,.04),black)
label('EVENT CO.',(0,-.215,.22),.065,cream);finish('storage_crate','Moveable aisle obstruction')
for i in range(1,4):
 cyl('Foot',(0,0,.02),.095,.04,brass);bar('Stem',(0,0,.04),(0,0,.24),.012,brass);cube('Sign',(0,0,.33),(.25,.035,.19),cream);label(str(i),(0,-.021,.29),.12);finish('table_sign_'+str(i),'Swappable table identity','convex')
cyl('Vase',(0,0,.13),.09,.26,cream)
for i in range(7):
 a=i*2.4;x=math.cos(a)*.13;y=math.sin(a)*.13;z=.35+(i%3)*.04
 bar('Stem',(0,0,.15),(x,y,z),.008,leaf);ball('Leaf',(x*.6,y*.6,.28),(.075,.03,.025),leaf)
 for j in range(5):ball('Petal',(x+math.cos(j*math.tau/5)*.034,y+math.sin(j*math.tau/5)*.034,z),(.035,.035,.025),pink if i%2 else white)
 ball('Flower centre',(x,y,z+.013),(.022,.022,.02),gold)
finish('flower_centerpiece','Table decoration','convex')
cube('Mixer housing',(0,0,.065),(.62,.40,.13),black)
for x in [-.22,-.11,0,.11,.22]:
 cube('Fader slot',(x,-.06,.135),(.015,.17,.009),silver);cube('Slider',(x,-.07+x*.15,.15),(.045,.035,.022),cream)
 for y in [.07,.14]:cyl('Knob',(x,y,.15),.018,.035,red if x==0 else teal)
finish('audio_mixer','Audio puzzle control surface')
cube('Speaker cabinet',(0,0,.49),(.45,.36,.98),black,.055)
for z,r in [(.32,.16),(.74,.09)]:
 o=cyl('Speaker cone',(0,-.188,z),r,.025,teal);o.rotation_euler.x=math.pi/2
 ball('Dust cap',(0,-.21,z),(r*.35,.025,r*.35),black)
finish('speaker','Ceremony audio equipment')
for n,m in [('red',red),('blue',blue),('yellow',gold)]:
 pts=[(.30*math.cos(i/47*math.tau*1.7),.23*math.sin(i/47*math.tau*1.7),.023) for i in range(48)];curve('Cable',pts,.013,m)
 for p in [pts[0],pts[-1]]:cube('Plug',p,(.065,.035,.035),black)
 finish('cable_'+n,'Display mesh for cable item; not a simulated flexible cable','none')
cube('Clipboard',(0,0,.17),(.24,.035,.34),wood);cube('Paper',(0,-.022,.17),(.21,.009,.29),cream);cube('Clip',(0,-.03,.32),(.09,.015,.04),silver)
for z in [.25,.21,.17,.13]:cube('Printed line',(0,-.028,z),(.15,.005,.005),teal,.001)
finish('clipboard','Coordinator prop / service manual')
cube('Display base',(0,0,.04),(.65,.45,.08),wood);bar('Display post',(0,0,.05),(0,0,1.35),.035,brass);cube('Display board',(0,0,1.43),(.78,.075,.65),teal);label('PHOTO TIME',(0,-.043,1.49),.092,cream);label('SMILE!',(0,-.043,1.31),.13,cream);finish('photo_display','Distraction activation target','convex')
for x in [-.9,.9]:
 cube('Arch foot',(x,0,.05),(.36,.5,.10),wood);bar('Arch post',(x,0,.1),(x,0,2.4),.045,brass)
bar('Arch top',(-.9,0,2.4),(.9,0,2.4),.045,brass)
for i in range(13):ball('Arch foliage',(-.9+i*.15,0,2.4),(.13,.10,.095),leaf);ball('Arch flower',(-.9+i*.15,-.08,2.45),(.055,.055,.05),pink if i%2 else cream)
finish('flower_arch','Photo corner and ceremony framing','compound')
cube('Stage',(0,0,.15),(3.8,1.8,.30),wood);cube('Stage trim',(0,-.91,.22),(3.8,.04,.05),brass);finish('stage_platform','Walkable stage')
for i in range(3):cube('Step',(0,i*.23,.05*(i+1)),(1.0,.24,.1*(i+1)),wood)
finish('stage_steps','Attach flush to stage; use ramp collision','compound')
cube('Module',(0,0,-.07),(2,2,.14),floor)
for y in [-.75,-.5,-.25,0,.25,.5,.75]:cube('Floor seam',(0,y,.001),(2,.009,.002),wood,.001)
finish('floor_tile_2m','Modular floor at ground Z=0')
cube('Wall',(0,0,1.5),(2,.16,3),wall);cube('Wainscot',(0,-.09,.45),(2,.04,.9),teal);cube('Dado rail',(0,-.12,.92),(2,.06,.055),brass);finish('wall_panel_2m','Solid venue wall')
for x in [-1.05,1.05]:cube('Door jamb',(x,0,1.5),(.30,.22,3),teal)
cube('Lintel',(0,0,2.8),(2.4,.22,.4),teal);label('STAFF ONLY',(0,-.12,2.76),.16,cream);finish('staff_doorway','Open 1.8m passage; use separate jamb collisions','compound')
cube('Shelving back',(0,.23,.85),(1.2,.05,1.7),teal)
for z in [.12,.65,1.18,1.7]:cube('Shelf',(0,0,z),(1.2,.50,.055),wood)
for x in [-.58,.58]:cube('Upright',(x,0,.85),(.045,.5,1.7),teal)
finish('storage_shelf','Back-of-house storage','compound')
# Character source created in the preceding study; execute only its mesh-building section.
worker_source=open(os.path.join(OUT,'source','build_worker.py'),encoding='utf-8').read()
ns={'__file__':os.path.join(OUT,'source','build_worker.py')}
worker_source=worker_source.replace("bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)",'')
exec(worker_source.split('# Export the actual character only')[0],ns)
current=list(ns['parts']);base=finish('worker_host','Static worker study; teal uniform. Requires skinning before animation','capsule')
for n,col,feature in [('worker_sound',(.12,.17,.29),'headset'),('worker_catering',(.44,.23,.075),'apron'),('worker_runner',(.28,.15,.24),'cap'),('coordinator',(.17,.065,.07),'clipboard')]:
 o=base.copy();o.data=base.data.copy();bpy.context.collection.objects.link(o);o.hide_render=False;o.hide_set(False);current=[o]
 replacement=material(n+'_uniform',col)
 for i,m in enumerate(o.data.materials):
  if m and m.name.startswith('Petrol uniform'):o.data.materials[i]=replacement
 if feature=='headset':
  curve('Headset band',[(-.28,0,1.93),(-.30,0,2.27),(.30,0,2.27),(.28,0,1.93)],.023,black);ball('Earpiece',(-.28,0,1.94),(.06,.08,.09),black);bar('Mic',(-.29,-.03,1.92),(-.10,-.30,1.80),.012,black)
 elif feature=='apron':cube('Apron',(0,-.21,1.02),(.48,.035,.50),cream,.045);cube('Apron pocket',(0,-.235,1.0),(.23,.025,.13),teal)
 elif feature=='cap':
  ball('Cap',(0,.015,2.30),(.34,.30,.20),gold);cube('Cap brim',(0,-.28,2.27),(.57,.27,.045),gold)
 else:
  cube('Supervisor clipboard',(.49,-.22,1.10),(.29,.045,.39),wood);cube('Supervisor paper',(.49,-.25,1.10),(.25,.01,.33),cream)
 finish(n,'Static same-base costume variant; not a separate rigged character','capsule')
# Arrange the asset inventory into a readable studio sheet.
def show(n,p,angle=0):
 o=items[n].copy();o.data=items[n].data;bpy.context.collection.objects.link(o);o.name=n+'_placed';o.hide_render=False;o.hide_set(False);o.location=p;o.rotation_euler.z=angle;return o
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
def studio(camera_pos,target,scale):
 for o in list(bpy.data.objects):
  if o.type in {'LIGHT','CAMERA'}:bpy.data.objects.remove(o,do_unlink=True)
 for p,power,size in [((-6,-8,14),2300,8),((9,-2,10),1900,7),((0,8,12),2600,6)]:
  bpy.ops.object.light_add(type='AREA',location=p);o=bpy.context.object;o.data.energy=power;o.data.size=size;aim(o,target)
 bpy.ops.object.camera_add(location=camera_pos);cam=bpy.context.object;aim(cam,target);cam.data.type='ORTHO';cam.data.ortho_scale=scale;bpy.context.scene.camera=cam
 s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=24;s.cycles.use_denoising=True;s.render.resolution_x=1600;s.render.resolution_y=1100;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.world.color=(.22,.22,.22)
def render(name):
 s=bpy.context.scene;s.render.filepath=os.path.join(OUT,'previews',name+'.png');bpy.ops.render.render(write_still=True)
characters=['worker_host','worker_sound','worker_catering','worker_runner','coordinator']
for i,n in enumerate(characters):show(n,(-3+i*1.5,2.7,0))
small=[n for n in items if n not in characters and n not in ['floor_tile_2m','wall_panel_2m','staff_doorway','flower_arch','stage_platform','stage_steps','storage_shelf']]
for i,n in enumerate(small):show(n,(-4+(i%6)*1.6,1-(i//6)*1.4,0))
current=[];cube('Sheet ground',(0,0,-.12),(200,200,.1),wall);sheetground=current.pop()
studio((10,-17,16),(0,-.6,.3),14);render('01_asset_inventory')
for o in list(bpy.data.objects):
 if o.name.endswith('_placed') or o==sheetground:bpy.data.objects.remove(o,do_unlink=True)
# Cutaway venue: 12m wide x 10m deep, with open front for inspection.
for x in [-5,-3,-1,1,3,5]:
 for y in [-4,-2,0,2,4]:show('floor_tile_2m',(x,y,0))
for x in [-5,-3,-1,1,3,5]:show('wall_panel_2m',(x,5,0))
for y in [-4,-2,0,2,4]:show('wall_panel_2m',(-6,y,0),math.pi/2)
show('stage_platform',(0,3.7,0));show('stage_steps',(0,2.2,0));show('flower_arch',(0,4.1,.3))
for x in [-1.5,1.5]:show('speaker',(x,3.3,.3))
show('sound_desk',(3.1,3.6,0));show('audio_mixer',(3.1,3.6,.95));show('cable_red',(3.3,2.8,.01))
for i,(x,y) in enumerate([(-3.4,1),(-3.4,-2.1),(0,-1.1)]):
 show('guest_table',(x,y,0));show('flower_centerpiece',(x,y,.84));show('table_sign_'+str(i+1),(x+.34,y,.84))
 for a in [0,math.pi/2,math.pi,3*math.pi/2]:show('guest_chair',(x+math.sin(a)*1.02,y+math.cos(a)*1.02,0),-a)
show('dessert_table',(4.3,.8,0));show('cake_stand',(4.3,.8,.945))
z=1.24
for n,h in [('cake_base',.23),('cake_middle',.20),('cake_top',.18)]:show(n,(4.3,.8,z));z+=h+.035
show('cake_topper',(4.3,.8,z));show('photo_display',(-4.9,-4,0))
# Backstage zone at front-right; partition opening stays visible.
show('wall_panel_2m',(4.8,-2,0));show('staff_doorway',(2.6,-2,0));show('prep_table',(4.6,-3.4,0));show('storage_shelf',(5,-4.65,0));show('storage_crate',(4,-4.3,0));show('mop_bucket',(3.2,-4.2,0));show('mop',(3.65,-4.3,0));show('cleaning_cloth',(4.3,-3.4,.90));show('clipboard',(4.9,-3.4,.9))
for n,p,a in [('worker_host',(1.3,.5,0),-.2),('worker_sound',(2.2,2.7,0),.2),('worker_catering',(3.6,-3,0),-.4),('worker_runner',(-1.7,-3.8,0),-.3),('coordinator',(-1.5,1.7,0),-.2)]:show(n,p,a)
studio((15,-21,18),(0,.2,.6),18)
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.context.scene.objects:
 if o.type=='MESH' and not o.hide_render:o.select_set(True)
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'wedding_venue_layout.glb'),use_selection=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'source','wedding_pack.blend'))
render('02_wedding_venue')
with open(os.path.join(OUT,'asset_manifest.json'),'w',encoding='utf-8') as f:json.dump({'units':'metres','source_up':'Z','gltf_up':'Y','character_front_blender':'-Y','assets':records,'map':'wedding_venue_layout.glb','status':'Static art prototype. No rigs, animations, collisions, gameplay scripts, or navigation baked.'},f,indent=2)
print('PACK_COMPLETE',len(records),'assets')
