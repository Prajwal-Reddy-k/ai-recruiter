interface SkeletonProps {
  width?: string;
  height?: string;
  className?: string;
}

export function Skeleton({ width, height = "1rem", className }: SkeletonProps) {
  return <span className={["skeleton", className ?? ""].filter(Boolean).join(" ")} style={{ width, height }} />;
}

export function JobCardSkeleton() {
  return (
    <div className="job-card">
      <Skeleton width="70%" height="1.25rem" />
      <Skeleton width="40%" />
      <Skeleton width="55%" />
      <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.5rem" }}>
        <Skeleton width="60px" height="1.5rem" />
        <Skeleton width="60px" height="1.5rem" />
        <Skeleton width="60px" height="1.5rem" />
      </div>
    </div>
  );
}

/** A generic row-of-lines skeleton for card lists (dashboard sections, notification lists). */
export function ListSkeleton({ rows = 3 }: { rows?: number }) {
  return (
    <div className="list-skeleton">
      {Array.from({ length: rows }).map((_, i) => (
        <div className="list-skeleton-row" key={i}>
          <Skeleton width="60%" height="1rem" />
          <Skeleton width="30%" height="0.85rem" />
        </div>
      ))}
    </div>
  );
}

/** A generic skeleton for `dashboard-table`-shaped tables. */
export function TableSkeleton({ rows = 4, columns = 4 }: { rows?: number; columns?: number }) {
  return (
    <div className="table-scroll">
      <table className="dashboard-table">
        <tbody>
          {Array.from({ length: rows }).map((_, r) => (
            <tr key={r}>
              {Array.from({ length: columns }).map((_, c) => (
                <td key={c}><Skeleton width={c === 0 ? "80%" : "60%"} /></td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
