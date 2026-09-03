import sqlite3, os

db = r'C:\Users\b1344\AppData\Local\Packages\37126GoldenPotato137.PotatoVN_8vtbc0gbd4jey\LocalState\pvn_data.db'
print('DB size:', os.path.getsize(db))
con = sqlite3.connect(f'file:{db}?mode=ro', uri=True)
cur = con.cursor()
cur.execute("SELECT name, type FROM sqlite_master WHERE type IN ('table','view') ORDER BY type, name")
objs = cur.fetchall()
print('OBJECTS:')
for name, typ in objs:
    print(f'  [{typ}] {name}')

print('\nSCHEMAS:')
for name, typ in objs:
    cur.execute(f"SELECT sql FROM sqlite_master WHERE name='{name}'")
    row = cur.fetchone()
    if row and row[0]:
        print(f'--- {name} ---')
        print(row[0][:800])
con.close()
