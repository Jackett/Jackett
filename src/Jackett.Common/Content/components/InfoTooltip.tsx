import { Tooltip } from "@geist-ui/core";
import { Info } from "@geist-ui/icons";

export default function InfoTooltip({ children }: React.PropsWithChildren<{}>) {
	return (
		<Tooltip text={children} placement="rightStart" hideArrow>
			<Info size={16} />
		</Tooltip>
	);
}
