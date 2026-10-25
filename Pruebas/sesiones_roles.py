"""Pruebas de sesiones, permisos y documentos. Crea datos solo en la API indicada."""
import urllib.request,urllib.error,http.cookiejar,json,os,time,datetime,base64
from pathlib import Path
BASE=os.environ.get('PV_TEST_URL','http://127.0.0.1:5081/')
PASSWORD=os.environ['PV_TEST_PASSWORD']
passed=[]
def request(method,path,data=None,token=None,status=200,opener=None,headers=None):
    h={'Content-Type':'application/json',**(headers or {})}
    if token:h['Authorization']='Bearer '+token
    r=urllib.request.Request(BASE+path,data=None if data is None else json.dumps(data).encode(),headers=h,method=method)
    try:
        with (opener.open(r,timeout=20) if opener else urllib.request.urlopen(r,timeout=20)) as response:code,raw=response.status,response.read()
    except urllib.error.HTTPError as error:code,raw=error.code,error.read()
    assert code==status,(method,path,code,raw.decode()[:500],status)
    return json.loads(raw) if raw else None

def ok(name):passed.append(name);print('PASS:',name,flush=True)
def login(name,password=PASSWORD):return request('POST','api/auth/login',{'usuario':name,'password':password})['token']
admin=login('admin');tag=str(int(time.time()))[-7:]
profile=request('GET','api/auth/session',token=admin);assert profile['esAdministrador']
# La cookie persiste sin entregar el JWT a JavaScript y requiere cabecera para escrituras.
jar=http.cookiejar.CookieJar();browser=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))
r=request('POST','api/auth/login',{'usuario':'admin','password':PASSWORD,'acceso':'administrador'},opener=browser,headers={'X-PV-Client':'1'})
assert 'token' not in r
cookie=list(jar)[0];assert cookie.name=='pv_sesion' and cookie.expires>time.time()+6*86400 and 'httponly' in {k.lower() for k in cookie._rest}
request('GET','api/auth/session',opener=browser)
request('POST','api/auth/logout',{},opener=browser,status=403)
request('POST','api/auth/logout',{},opener=browser,headers={'X-PV-Client':'1'},status=204)
request('GET','api/auth/session',opener=browser,status=401)
ok('Cookie HttpOnly persistente, recuperación de sesión y protección CSRF')
api=login('admin');request('POST','api/auth/logout',{},token=api,status=204);request('GET','api/auth/session',token=api,status=401)
parts=admin.split('.');parts[2]=('A' if parts[2][0]!='A' else 'B')+parts[2][1:]
request('GET','api/auth/session',token='.'.join(parts),status=401)
ok('JWT firmado; cierre revoca el token incluso si se conserva una copia')
baseemployee=request('GET','api/empleados',token=admin)[0]
actors={}
for index,(role,v,c,a) in enumerate([('vendedor',True,False,False),('comprador',False,True,False),('administrativo',False,False,True),('ambos',True,True,False)]):
    employee=dict(baseemployee);employee.update(id=0,nombres='Prueba '+role,apellidos=tag,dpi='44444'+str(index)+tag,activo=True,fechaEgreso=None,sueldo=1200)
    employee=request('POST','api/empleados',employee,admin,201)
    account={'nombre':'QA_'+role.upper()+'_'+tag,'empleadoId':employee['id'],'vendedor':v,'comprador':c,'administrativo':a,'activo':True,'password':'PruebaRoles2026!'}
    created=request('POST','api/cuentas',account,admin)
    account['id']=created['id'];actors[role]={'employee':employee,'account':account,'token':login(account['nombre'],'PruebaRoles2026!')}
    menu=request('GET','api/menu',token=actors[role]['token']);paths={x['ruta'] for x in menu}
    assert ('documentos/ventas' in paths)==v
    assert ('documentos/compras' in paths)==c
    assert ('catalogos/empleados' in paths)==a
    assert 'administracion/cuentas' not in paths
ok('Cuatro combinaciones de roles y menú filtrado por permisos')
seller=actors['vendedor']['token'];buyer=actors['comprador']['token'];hr=actors['administrativo']['token'];dual=actors['ambos']['token']
request('GET','api/clientes',token=seller,status=403)
request('GET','api/cuentas',token=hr,status=403)
request('GET','api/empleados',token=buyer,status=403)
request('GET','api/productos',token=hr,status=403)
request('DELETE','api/productos/1',token=seller,status=403)
request('POST','api/productos',{'nombre':'No permitido','marcaId':1,'descripcion':'No permitido','precioCosto':1,'precioVenta':1,'existencia':0},buyer,403)
request('GET','api/documentos/compras',token=seller,status=403)
request('GET','api/documentos/ventas',token=buyer,status=403)
request('GET','api/documentos/ventas',token=dual);request('GET','api/documentos/compras',token=dual)
ok('Permisos aplicados en API: vendedor no compra, comprador no vende y RRHH no altera cuentas')
cf=request('GET','api/clientes/por-nit?nit=C%2FF',token=seller);assert cf['encontrado'] and cf['cliente']['nit']=='CF'
unknown=request('GET','api/clientes/por-nit?nit=9'+tag,token=seller);assert not unknown['encontrado']
client=request('POST','api/clientes',{'nombres':'Cliente','apellidos':'NIT '+tag,'nit':'9'+tag+'-K','telefono':'55551234','correoElectronico':'qa@example.com'},seller,201)
found=request('GET','api/clientes/por-nit?nit=9'+tag+'K',token=seller);assert found['cliente']['id']==client['id']
request('POST','api/clientes',client,seller,400)
request('PUT','api/clientes/'+str(client['id']),client,seller,403)
ok('Búsqueda exacta NIT normalizado, CF, no encontrado y alta de cliente desde ventas')
products=request('GET','api/productos',token=buyer);product=next(p for p in products if p['existencia']>1)
provider=request('GET','api/proveedores',token=buyer)[0]
num=int(tag);today=datetime.date.today().isoformat()
purchase={'numero':num,'terceroId':provider['id'],'fecha':today,'detalles':[{'productoId':product['id'],'cantidad':2,'precio':100}]}
preview=request('POST','api/documentos/compras/previsualizar',purchase,buyer)
assert preview['id']==0 and preview['total']==200 and preview['nit']==provider['nit']
assert request('GET','api/productos/'+str(product['id']),token=admin)['existencia']==product['existencia']
saved=request('POST','api/documentos/compras',purchase,buyer,201)
assert saved['id']>0 and saved['total']==200
request('POST','api/documentos/compras/previsualizar',purchase,seller,403)
ok('Vista previa de compra sin guardar y confirmación con detalle exacto')
sale={'numero':num,'serie':'Q','terceroId':client['id'],'empleadoId':baseemployee['id'],'fecha':today,'detalles':[{'productoId':product['id'],'cantidad':1,'precio':1}]}
preview=request('POST','api/documentos/ventas/previsualizar',sale,seller)
assert preview['total']==125 and preview['nit']==client['nit'] and actors['vendedor']['employee']['nombres'] in preview['empleado']
saved=request('POST','api/documentos/ventas',sale,seller,201)
assert saved['total']==125 and saved['nit']==client['nit']
assert saved['id'] in [x['id'] for x in request('GET','api/documentos/ventas',token=seller)]
assert saved['id'] not in [x['id'] for x in request('GET','api/documentos/ventas',token=dual)]
request('GET','api/documentos/ventas/'+str(saved['id']),token=dual,status=404)
request('POST','api/documentos/ventas/previsualizar',sale,buyer,403)
# El nombre y NIT del documento confirmado sobreviven a cambios en el cliente.
client['nombres']='Nombre cambiado';request('PUT','api/clientes/'+str(client['id']),client,admin,204)
assert request('GET','api/documentos/ventas/'+str(saved['id']),token=seller)['tercero']==saved['tercero']
ok('Factura con NIT, precio oficial, vendedor autenticado, historial propio e instantánea del cliente')
account=actors['ambos']['account'];old=actors['ambos']['token'];account['comprador']=False;account['password']=None
request('PUT','api/cuentas/'+str(account['id']),account,admin)
request('GET','api/auth/session',token=old,status=401)
new=login(account['nombre'],'PruebaRoles2026!');request('GET','api/documentos/compras',token=new,status=403)
ok('Cambio de roles revoca sesiones previas y aplica permisos nuevos')
account['password']='123';request('PUT','api/cuentas/'+str(account['id']),account,admin,400)
account['password']='';request('PUT','api/cuentas/'+str(account['id']),account,admin)
login(account['nombre'],'PruebaRoles2026!')
ok('Cuenta rechaza claves cortas y conserva la clave cuando se deja vacía')
employee=actors['vendedor']['employee'];employee.update(sueldo=2500,activo=False,fechaEgreso=today)
request('PUT','api/empleados/'+str(employee['id']),employee,hr,204)
actual=request('GET','api/empleados/'+str(employee['id']),token=hr);assert actual['sueldo']==2500 and not actual['activo']
request('GET','api/auth/session',token=seller,status=401)
request('POST','api/auth/login',{'usuario':actors['vendedor']['account']['nombre'],'password':'PruebaRoles2026!'},status=401)
ok('Administrativo modifica sueldo y egreso; egreso revoca el acceso del empleado')
employee.update(activo=True,fechaEgreso=None);request('PUT','api/empleados/'+str(employee['id']),employee,hr,204)
login(actors['vendedor']['account']['nombre'],'PruebaRoles2026!')
ok('Reingreso permite un nuevo acceso sin restaurar sesiones revocadas')
Path(__file__).with_name('resultado-sesiones-roles.json').write_text(json.dumps({'fechaUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'api':BASE,'aprobados':passed},ensure_ascii=False,indent=2),encoding='utf-8')
print('PRUEBAS NUEVAS COMPLETADAS')
