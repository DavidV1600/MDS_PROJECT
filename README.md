# **Cheapest Items Nearby Finder**

---

## **Componenta echipă**

- **Voinescu David-Ioan**
- **Ghena-Ionescu Alexandru**

---

## **Epic Stories**

1. **Căutarea celor mai ieftine produse**
   - Utilizatorul poate căuta un produs și va primi o listă cu toate magazinele care au acel produs, împreună cu prețurile și cantitățile disponibile, pe care le poate sorta crescător/descrescător după cum dorește.

2. **Afișarea magazinului cel mai apropiat**
   - Sistemul afișează magazinul cel mai apropiat în funcție de locația utilizatorului sau de locația introdusă de utilizator, cu durata estimată până la el în funcție de mijlocul de deplasare și cu cea mai optimă rută spre acesta.

3. **Calcularea celui mai ieftin coș de cumpărături**
   - Utilizatorul poate introduce o listă de produse cu cantitățile dorite și va primi calculul celui mai ieftin coș pentru fiecare magazin, inclusiv suma totală a prețurilor pentru fiecare magazin și produsele selectate pentru fiecare coș.

---

## **User Stories**

1. **Căutare produse disponibile la magazine**
   - **Descriere:** Ca utilizator, vreau să pot vedea toate produsele de un anumit tip disponibile la diverse magazine, astfel încât să pot alege cel mai potrivit magazin pentru a cumpăra produsul dorit.

2. **Sortare produse după cantitate**
   - **Descriere:** Ca utilizator, vreau să pot vedea toate produsele de un tip disponibile la magazine sortate după cantitate, atât în ordine crescătoare cât și descrescătoare, astfel încât să pot găsi rapid produsele care îndeplinesc nevoile mele cantitative.

3. **Sortare produse după preț**
   - **Descriere:** Ca utilizator, vreau să pot vedea toate produsele de un tip disponibile la magazine sortate după preț, astfel încât să pot alege cel mai ieftin produs disponibil.

4. **Sortare produse în ordine alfabetică**
   - **Descriere:** Ca utilizator, vreau să pot vedea produsele în ordine alfabetică, astfel încât să găsesc mai ușor un anumit produs pe care îl caut.

5. **Vizualizare magazin cel mai apropiat**
   - **Descriere:** Ca utilizator, vreau să pot vedea unde se află cel mai apropiat magazin care are produsele pe care le doresc, astfel încât să pot economisi timp și efort.

6. **Detalii rută și durată până la magazin**
   - **Descriere:** Ca utilizator, vreau să văd care este durata, ruta și modurile în care pot ajunge la magazinul dorit, astfel încât să pot planifica eficient drumul meu.

7. **Setare locație personalizată**
   - **Descriere:** Ca utilizator, vreau să pot seta o locație personalizată, astfel încât să pot vedea rutele și magazinele disponibile în funcție de acea locație, indiferent de locul în care mă aflu.

8. **Construire coș de cumpărături**
   - **Descriere:** Ca utilizator, vreau să pot construi un coș de cumpărături și să văd produsele de acest tip disponibile la fiecare magazin, astfel încât să pot compara opțiunile și alege cea mai bună ofertă.

9. **Calcul cel mai ieftin coș de cumpărături**
   - **Descriere:** Ca utilizator, vreau să văd cel mai ieftin coș de cumpărături pentru fiecare magazin, incluzând prețul total al coșului, astfel încât să pot face achiziții economice.

10. **Alegerea cantităților pentru coșul de cumpărături**
    - **Descriere:** Ca utilizator, vreau să pot alege cantitatea pentru fiecare obiect din coșul de cumpărături, astfel încât să pot personaliza achizițiile în funcție de nevoile mele.

---

## **Backlog**

1. **Integrarea API-urilor pentru geo-localizare și prețuri produse**
   - **Descriere:** Implementarea integrării cu API-uri care oferă date de geo-localizare automate pentru utilizator.
  
2. **Integrarea unei locații custom**
   - **Descriere:** Adăugarea opțiunii pentru utilizator de a putea selecta din ce locație dorește să fie calculate rutele către magazine.
  
3. **Integrarea completă a hărților**
   - **Descriere:** Adăugarea unor butoane ce lasă utilizatorul să își selecteze modul de transport dorit și afișarea rutei împreună cu timpul estimat.
  
4. **Adăugarea web-crawlers**
   - **Descriere:** Implementarea unor scripturi de Python care accesează paginile cu produse ale magazinelor și selectează codul HTML relevant pentru produse.

5. **Îmbunătățirea web-crawlers**
   - **Descriere:** Adăugarea unei opțiuni atât pentru codurile de Python, cât și pentru utilizator care îl întreabă dacă dorește ca inputul lui să fie găsit exact cum a fost scris sau să fie incluse și alte iteme cu denumiri apropiate.

6. **Dezvoltarea interfeței de utilizator**
   - **Descriere:** Dezvoltarea unei interfețe de utilizator pentru căutarea și afișarea produselor din diverse magazine, sortate după criteriile dorite.

7. **Implementarea unui coș de cumpărături**
   - **Descriere:** Adăugarea unui coș de cumpărături care adaugă automat cel mai ieftin produs care este corespunzător cu numele și cantitatea introdusă de utilizator.

8. **Optimizarea găsirii obiectelor dorite**
   - **Descriere:** Folosirea unei baze de date care stochează produsele deja căutate pentru o perioadă limitată de timp, dar care ajută găsirea instantă a obiectelor deja căutate.

## **Diagrama UML**
<img src="https://github.com/DavidV1600/MDS_PROJECT/blob/master/uml2.png?raw=true">

## **Diagrama WorkFlow**
<img src="https://github.com/DavidV1600/MDS_PROJECT/blob/master/workflow.png?raw=true">
