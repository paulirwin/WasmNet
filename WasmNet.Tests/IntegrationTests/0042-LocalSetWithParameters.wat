;; invoke: localset (i32:3)
;; expect: (i32:1)

(module
    (func (export "localset") (param $x i32) (result i32) (local $y i32)
        i32.const 2
        local.set $y
        local.get $x
        local.get $y
        i32.sub
    )
)