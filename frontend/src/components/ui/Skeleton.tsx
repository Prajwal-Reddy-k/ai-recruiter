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
