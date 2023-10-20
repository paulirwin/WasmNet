;; invoke: incGlobal
;; invoke: getGlobal
;; expect: (i32:43)

(module
   (global $g (mut i32) (i32.const 42))
   (func (export "getGlobal") (result i32)
        (global.get $g))
   (func (export "incGlobal")
        (global.set $g
            (i32.add (global.get $g) (i32.const 1))))
)
