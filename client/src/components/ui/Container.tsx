import type { ComponentPropsWithoutRef, ElementType } from 'react';

import { classNames } from '@/utils/classNames';

type ContainerWidth = 'md' | 'lg' | 'xl';

type ContainerProps<T extends ElementType> = {
  as?: T;
  width?: ContainerWidth;
} & ComponentPropsWithoutRef<T>;

export function Container<T extends ElementType = 'div'>({
  as,
  className,
  width = 'xl',
  ...props
}: ContainerProps<T>) {
  const Component = as ?? 'div';

  return <Component className={classNames('ui-container', `ui-container--${width}`, className)} {...props} />;
}
