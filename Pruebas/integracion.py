import json, urllib.request, urllib.error, datetime, concurrent.futures, time, os
from pathlib import Path
base=os.environ.get('PV_TEST_URL','http://127.0.0.1:5080/')
token=None
results=[]
def call(method,path,data=None,status=200,auth=True):
    headers={'Content-Type':'application/json'}
    if token and auth:headers['Authorization']='Bearer '+token
    req=urllib.request.Request(base+path,data=None if data is None else json.dumps(data).encode(),headers=headers,method=method)
    try:
        with urllib.request.urlopen(req,timeout=20) as r: code,raw=r.status,r.read()
    except urllib.error.HTTPError as e: code,raw=e.code,e.read()
    assert code==status, (method,path,code,raw.decode()[:500],status)
    return json.loads(raw) if raw else None

def ok(name):results.append(name);print('PASS:',name,flush=True)
call('GET','api/productos',status=401,auth=False)
call('POST','api/auth/login',{'usuario':'admin','password':'incorrecta'},status=401,auth=False)
token=call('POST','api/auth/login',{'usuario':'admin','password':os.environ['PV_TEST_PASSWORD']},auth=False)['token']
assert len(call('GET','api/menu'))>=9
ok('JWT, rechazo sin sesión y menú padre-hijo')
tag=str(int(time.time()))[-7:]
puesto=call('POST','api/puestos',{'nombre':'Caja '+tag},201)
marca=call('POST','api/marcas',{'nombre':'Marca '+tag},201)
cliente=call('POST','api/clientes',{'nombres':'Cliente','apellidos':'Demo '+tag,'nit':'C'+tag,'genero':True,'telefono':'55551234','correoElectronico':'demo@example.com'},201)
proveedor=call('POST','api/proveedores',{'nombre':'Distribuidora '+tag,'nit':'P'+tag,'direccion':'Zona 1','telefono':'55552345'},201)
empleado=call('POST','api/empleados',{'nombres':'Ana','apellidos':'Demo '+tag,'dpi':'123456'+tag,'direccion':'Zona 2','telefono':'55553456','genero':False,'fechaNacimiento':'2000-01-01','fechaInicioLabores':'2024-01-01','puestoId':puesto['id']},201)
producto=call('POST','api/productos',{'nombre':'Teclado '+tag,'marcaId':marca['id'],'descripcion':'Teclado para demostración','precioCosto':80,'precioVenta':100,'existencia':20},201)
for route,row,prop in [('puestos',puesto,'nombre'),('marcas',marca,'nombre'),('clientes',cliente,'nombres'),('proveedores',proveedor,'nombre'),('empleados',empleado,'nombres'),('productos',producto,'nombre')]:
    row[prop]+=' editado'
    call('PUT',f'api/{route}/{row["id"]}',row,204)
    actual=call('GET',f'api/{route}/{row["id"]}')
    assert actual[prop]==row[prop]
    assert any(x['id']==row['id'] for x in call('GET',f'api/{route}?buscar=editado'))
    if route=='productos':producto=actual
ok('Crear, consultar, editar y buscar en los seis CRUD')
call('POST','api/puestos',{'nombre':puesto['nombre']},409)
call('POST','api/clientes',{'nombres':'','apellidos':'','nit':'','telefono':'1','correoElectronico':'invalido'},400)
call('DELETE',f'api/puestos/{puesto["id"]}',status=409)
call('DELETE',f'api/marcas/{marca["id"]}',status=409)
ok('Validaciones, duplicados y protección de relaciones')
# Independent records exercise DELETE on every catalog without leaving orphan references.
for route,original,prop in [('clientes',cliente,'nit'),('proveedores',proveedor,'nit'),('empleados',empleado,'dpi'),('productos',producto,'nombre'),('puestos',puesto,'nombre'),('marcas',marca,'nombre')]:
    row=original.copy();row['id']=0
    row[prop]=('987654'+tag) if route=='empleados' else row[prop]+'X'
    if route=='productos':row['revision']=0
    fresh=call('POST','api/'+route,row,201)
    call('DELETE',f'api/{route}/{fresh["id"]}',status=204)
    call('GET',f'api/{route}/{fresh["id"]}',status=404)
ok('Eliminar y verificar eliminación en los seis CRUD')
num=int(tag)
compra={'numero':num,'terceroId':proveedor['id'],'fecha':datetime.date.today().isoformat(),'detalles':[{'productoId':producto['id'],'cantidad':10,'precio':100}]}
call('POST','api/documentos/compras',compra,201)
p=call('GET',f'api/productos/{producto["id"]}')
assert p['existencia']==30 and p['precioCosto']==100 and p['precioVenta']==125
ok('Compra: 20 + 10 = 30; costo Q100 y venta Q125')
call('PUT',f'api/productos/{producto["id"]}',producto,409)
ok('Rechazo de edición obsoleta de inventario')
venta={'numero':num,'serie':'A','terceroId':cliente['id'],'empleadoId':empleado['id'],'fecha':datetime.date.today().isoformat(),'detalles':[{'productoId':producto['id'],'cantidad':5,'precio':1}]}
call('POST','api/documentos/ventas',venta,201)
p=call('GET',f'api/productos/{producto["id"]}')
assert p['existencia']==25
v=next(x for x in call('GET','api/documentos/ventas') if x['numero']==num)
assert v['total']==625 and v['detalles'][0]['precio']==125
ok('Venta: stock 25 y total Q625; precio manipulado ignorado')
venta['numero']+=1;venta['detalles'][0]['cantidad']=26
call('POST','api/documentos/ventas',venta,409)
assert call('GET',f'api/productos/{producto["id"]}')['existencia']==25
ok('Existencia insuficiente rechazada sin modificar inventario')
# Failure at unique document constraint must roll back stock and prices.
call('POST','api/documentos/compras',compra,409)
assert call('GET',f'api/productos/{producto["id"]}')['existencia']==25
ok('Documento duplicado revierte toda la transacción')
# Two concurrent sales of 20 units with only 25 available: exactly one can succeed.
def concurrent_sale(n):
    request=venta.copy();request['numero']=num+n;request['detalles']=[{'productoId':producto['id'],'cantidad':20,'precio':125}]
    try:
        call('POST','api/documentos/ventas',request,201);return 201
    except AssertionError as ex:
        assert ex.args[0][2]==409,ex
        return 409
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:codes=list(pool.map(concurrent_sale,[2,3]))
assert sorted(codes)==[201,409]
assert call('GET',f'api/productos/{producto["id"]}')['existencia']==5
ok('Dos ventas simultáneas: una aceptada, otra rechazada; stock final 5')

call('POST','api/documentos/compras',{'numero':num+90,'terceroId':proveedor['id'],'detalles':None},400)
invalid=compra.copy();invalid['numero']=num+91;invalid['detalles']=[{'productoId':producto['id'],'cantidad':0,'precio':10}]
call('POST','api/documentos/compras',invalid,400)
invalid['detalles']=[{'productoId':producto['id'],'cantidad':1,'precio':10}]*2
call('POST','api/documentos/compras',invalid,400)
invalid['detalles']=[{'productoId':2147483647,'cantidad':1,'precio':10}]
call('POST','api/documentos/compras',invalid,400)
ok('Detalle nulo, cantidad cero, producto duplicado y referencia inexistente rechazados')
segundo=call('POST','api/productos',{'nombre':'Mouse '+tag,'marcaId':marca['id'],'descripcion':'Segundo producto de prueba','precioCosto':40,'precioVenta':50,'existencia':2},201)
multiple=compra.copy();multiple['numero']=num+100;multiple['detalles']=[{'productoId':producto['id'],'cantidad':2,'precio':100},{'productoId':segundo['id'],'cantidad':3,'precio':40}]
call('POST','api/documentos/compras',multiple,201)
assert call('GET',f'api/productos/{producto["id"]}')['existencia']==7
assert call('GET',f'api/productos/{segundo["id"]}')['existencia']==5
multi_sale=venta.copy();multi_sale['numero']=num+100;multi_sale['detalles']=[{'productoId':producto['id'],'cantidad':1,'precio':125},{'productoId':segundo['id'],'cantidad':2,'precio':50}]
call('POST','api/documentos/ventas',multi_sale,201)
assert next(x for x in call('GET','api/documentos/ventas') if x['numero']==num+100)['total']==225
call('POST','api/documentos/ventas',multi_sale,409)
assert call('GET',f'api/productos/{producto["id"]}')['existencia']==6
assert call('GET',f'api/productos/{segundo["id"]}')['existencia']==3
ok('Compra y venta con varios productos; rollback de todas las líneas ante duplicado')
def upload(content,status):
    boundary='PvTestBoundary2026'
    body=(f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="imagen.png"\r\nContent-Type: image/png\r\n\r\n').encode()+content+f'\r\n--{boundary}--\r\n'.encode()
    req=urllib.request.Request(base+'api/imagenes',data=body,method='POST',headers={'Authorization':'Bearer '+token,'Content-Type':'multipart/form-data; boundary='+boundary})
    try:
        with urllib.request.urlopen(req) as r:code,raw=r.status,r.read()
    except urllib.error.HTTPError as e:code,raw=e.code,e.read()
    assert code==status,(code,raw)
    return json.loads(raw)
asset=Path(__file__).resolve().parent.parent/'wwwroot/images/productos/demo-teclado.png'
png=asset.read_bytes();url=upload(png,200)['url']
with urllib.request.urlopen(base+url.lstrip('/')) as r:assert r.read()==png
upload(b'<svg><script>invalid</script></svg>',400)
p=call('GET',f'api/productos/{segundo["id"]}');p['imagen']=url
call('PUT',f'api/productos/{segundo["id"]}',p,204)
assert call('GET',f'api/productos/{segundo["id"]}')['imagen']==url
ok('Carga real de PNG, ruta persistida, imagen accesible y contenido inválido rechazado')

Path(__file__).with_name('resultado-integracion.json').write_text(json.dumps({'passed':results,'sample':{'producto':producto['id'],'cliente':cliente['id'],'empleado':empleado['id'],'proveedor':proveedor['id']}},ensure_ascii=False,indent=2),encoding='utf-8')
print('TODAS LAS PRUEBAS COMPLETADAS')
