import type { DetailProgrammeItem, ResumeSemaineJour } from '@/types/resultat';

interface ProgrammeDetailProps {
  detailProgramme: DetailProgrammeItem[];
  resumeSemaine: ResumeSemaineJour[];
}

export function ProgrammeDetail({ detailProgramme, resumeSemaine }: ProgrammeDetailProps) {
  return (
    <div className="space-y-6 p-5">
      {/* Semaine types breakdown */}
      {detailProgramme.length > 0 && (
        <div>
          <h4 className="mb-2 font-data text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Programme par semaine type
          </h4>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-left">
                <th className="py-2 pl-4 pr-3 font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                  Semaine Type
                </th>
                <th className="py-2 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                  Blocs
                </th>
                <th className="py-2 pr-4 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                  HDV (h)
                </th>
              </tr>
            </thead>
            <tbody>
              {detailProgramme.map((dp, i) => (
                <tr key={i} className="border-b last:border-0">
                  <td className="py-2 pl-4 pr-3 font-medium">{dp.item1}</td>
                  <td className="py-2 pr-3 text-right font-data">{dp.item2}</td>
                  <td className="py-2 pr-4 text-right font-data">{dp.item3.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Day-of-week summary */}
      {resumeSemaine.length > 0 && (
        <div>
          <h4 className="mb-2 font-data text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Resume par jour de semaine
          </h4>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-left">
                <th className="py-2 pl-4 pr-3 font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                  Jour
                </th>
                <th className="py-2 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                  Blocs
                </th>
                <th className="py-2 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                  HDV (h)
                </th>
                <th className="py-2 pr-4 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                  TS (h)
                </th>
              </tr>
            </thead>
            <tbody>
              {resumeSemaine.map((rs) => (
                <tr key={rs.jour} className="border-b last:border-0">
                  <td className="py-2 pl-4 pr-3 font-medium">{rs.jourNom}</td>
                  <td className="py-2 pr-3 text-right font-data">{rs.nbBlocs}</td>
                  <td className="py-2 pr-3 text-right font-data">{rs.hdv.toFixed(2)}</td>
                  <td className="py-2 pr-4 text-right font-data">{rs.ts.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {detailProgramme.length === 0 && resumeSemaine.length === 0 && (
        <p className="py-4 text-center text-sm text-muted-foreground">
          Aucune donnee programme disponible.
        </p>
      )}
    </div>
  );
}
