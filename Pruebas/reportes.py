"""Verificaciones de solo lectura; no crea ni modifica datos comerciales."""
import os,json,urllib.request,urllib.error,datetime
from decimal import Decimal
from pathlib import Path
BASE=os.environ.get('PV_TEST_URL','http://127.0.0.1:5080/').rstrip('/')+'/'
def req(path,token=None,data=None,status=200):
    headers={'Content-Type':'application/json'}
    if token: headers['Authorization']='Bearer '+token
    r=urllib.request.Request(BASE+path,headers=headers,data=None if data is None else json.dumps(data).encode())
    try:
        with urllib.request.urlopen(r,timeout=20) as response: code,raw=response.status,response.read()
    except urllib.error.HTTPError as e: code,raw=e.code,e.read()
    assert code==status,(path,code,raw.decode()[:500])
    return json.loads(raw,parse_float=Decimal) if raw else None
passed=[]
def ok(s): passed.append(s);print('PASS:',s)
req('api/reportes/productos',status=401)
admin=req('api/auth/login',data={'usuario':'admin','password':os.environ['PV_TEST_PASSWORD']})['token']
products=req('api/productos',admin)
r=req('api/reportes/productos',admin)
assert len(r['productos'])==len(products)
assert sum(x['existencia'] for x in r['productos'])==sum(x['existencia'] for x in products)
assert all(x['marca'] for x in r['productos'])
ok('Productos unidos con marcas; cantidad y unidades coinciden con el catálogo')
inv=req('api/reportes/existencias',admin)['productos']
for p in inv:
    assert p['valorCosto']==p['existencia']*p['precioCosto']
    assert p['valorVenta']==p['existencia']*p['precioVenta']
for state in ('bajo','agotado'):
    rows=req('api/reportes/existencias?estado='+state,admin)['productos']
    expected=[p for p in products if (p['existencia']<=5 if state=='bajo' else p['existencia']==0)]
    assert {p['id'] for p in rows}=={p['id'] for p in expected}
assert not req('api/reportes/productos?buscar=ZZZ_SIN_COINCIDENCIA_QA',admin)['productos']
ok('Valoración de inventario, filtros de stock y resultados vacíos')
for tipo in ('compras','ventas'):
    docs=req('api/documentos/'+tipo,admin)
    rows=req('api/reportes/'+tipo,admin)['operaciones']
    assert {d['id'] for d in rows}=={d['id'] for d in docs}
    expected={d['id']:sum(line['cantidad']*line['precio'] for line in d['detalles']) for d in docs}
    assert all(d['total']==expected[d['id']] for d in rows)
    for d in rows:
        day=d['fecha'][:10]
        filtered=req(f'api/reportes/{tipo}?desde={day}&hasta={day}',admin)['operaciones']
        assert all(x['fecha'][:10]==day for x in filtered)
        assert d['id'] in {x['id'] for x in filtered}
    assert not req('api/reportes/'+tipo+'?desde=2099-01-01&hasta=2099-01-02',admin)['operaciones']
ok('Compras y ventas: totales desde detalles, documentos relacionados y fechas inclusivas')
req('api/reportes/ventas?desde=2026-10-22&hasta=2026-01-01',admin,status=400)
req('api/reportes/productos?estado=incorrecto',admin,status=400)
req('api/reportes/productos?desde=fecha-invalida',admin,status=400)
req('api/reportes/desconocido',admin,status=404)
assert any(m['ruta']=='reportes' for m in req('api/menu',admin))
ok('Validaciones de API y opción Reportes en menú dinámico')
employee=req('api/auth/login',data={'usuario':'EMPLEADO','password':os.environ['PV_EMPLOYEE_PASSWORD']})['token']
for tipo in ('productos','existencias','compras','ventas'):req('api/reportes/'+tipo,employee,status=403)
assert not any(m['ruta']=='reportes' for m in req('api/menu',employee))
ok('Informes globales protegidos: acceso anónimo y empleado rechazados')
Path(__file__).with_name('resultado-reportes.json').write_text(json.dumps({'fechaUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'pruebas':passed,'soloLecturaComercial':True},ensure_ascii=False,indent=2),encoding='utf-8')
