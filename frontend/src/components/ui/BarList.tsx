export interface BarListItem {
  name: string;
  count: number;
}

/** Dependency-free horizontal bar chart — plain divs sized by CSS width%, no charting
 * library. Always pair with an equivalent data table for accessibility. */
export default function BarList({ items }: { items: BarListItem[] }) {
  const max = Math.max(1, ...items.map((i) => i.count));
  return (
    <div className="bar-chart">
      {items.map((item) => (
        <div className="bar-chart-row" key={item.name}>
          <span className="bar-chart-label">{item.name}</span>
          <div className="bar-chart-track">
            <div className="bar-chart-fill" style={{ width: `${(item.count / max) * 100}%` }} />
          </div>
          <span className="bar-chart-value">{item.count}</span>
        </div>
      ))}
    </div>
  );
}
