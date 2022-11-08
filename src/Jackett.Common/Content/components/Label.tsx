import { Text } from "@geist-ui/core";

export default function Label({ children }: React.PropsWithChildren<{}>) {
	return (
		<Text
			font="0.9em"
			type="secondary"
			style={{
				fontWeight: "normal",
				lineHeight: 1.5,
				marginBottom: "0.5em",
				padding: "0 0 0 1px",
			}}
		>
			{children}
		</Text>
	);
}
