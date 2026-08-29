import { getInitials } from "../../utils/format";

interface AvatarProps {
  name: string;
  size?: number;
}

export default function Avatar({ name, size = 36 }: AvatarProps) {
  return (
    <span
      className="ui-avatar"
      style={{ width: size, height: size, fontSize: size * 0.4 }}
      aria-hidden="true"
    >
      {getInitials(name)}
    </span>
  );
}
